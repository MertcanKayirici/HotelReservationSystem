using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Confluent.Kafka;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HotelReservationSystem.Infrastructure.Kafka;
using System.Text.Json;

public class ReservationConsumer : BackgroundService
{
    private readonly IKafkaProducerFactory _producerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<ReservationConsumer> _logger;

    public ReservationConsumer(
            IServiceProvider serviceProvider,
            IConfiguration config,
            ILogger<ReservationConsumer> logger,
            IKafkaProducerFactory producerFactory)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            _logger = logger;
            _producerFactory = producerFactory;
        }


    private async Task<bool> SendToDlqAsync(
        IKafkaProducer producer,
        string dlqTopic,
        string payload,
        TopicPartitionOffset offset,
        CancellationToken cancellationToken)
    {
        try
        {
            await producer.SendAsync(
                dlqTopic,
                payload,
                cancellationToken);

            _logger.LogWarning(
                "Message at {Offset} sent to DLQ topic {DlqTopic}.",
                offset,
                dlqTopic);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send message at {Offset} to DLQ topic {DlqTopic}.",
                offset,
                dlqTopic);

            return false;
        }
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_config.GetValue<bool>("Kafka:Enabled"))
        {
            _logger.LogWarning("Kafka is disabled. Consumer will not start.");
            return;
        }

        var dlqTopic = _config["Kafka:DlqTopic"];

        if (string.IsNullOrWhiteSpace(dlqTopic))
        {
            _logger.LogError(
                "Kafka DLQ topic is missing. Reservation consumer will not start.");
            return;
        }

        var bootstrapServers = _config["Kafka:BootstrapServers"];
        var topic = _config["Kafka:Topic"];

        if (string.IsNullOrWhiteSpace(bootstrapServers) ||
            string.IsNullOrWhiteSpace(topic))
        {
            _logger.LogError("Kafka bootstrap server or topic is missing.");
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "reservation-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer =
            new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

        IKafkaProducer? dlqProducer = null;

        try
        {
            dlqProducer = _producerFactory.Create(bootstrapServers);

            consumer.Subscribe(topic);
            _logger.LogInformation("Reservation consumer started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string>? result = null;

                try
                {
                    result = consumer.Consume(stoppingToken);

                    CreateReservationDto? dto;

                    try
                    {
                        dto = JsonSerializer.Deserialize<CreateReservationDto>(
                            result.Message.Value,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(
                            ex,
                            "Invalid JSON in reservation event at {Offset}.",
                            result.TopicPartitionOffset);

                        var sentToDlq = await SendToDlqAsync(
                            dlqProducer!,
                            dlqTopic,
                            result.Message.Value,
                            result.TopicPartitionOffset,
                            stoppingToken);

                        if (sentToDlq)
                            consumer.Commit(result);

                        continue;
                    }

                    if (dto == null ||
                        string.IsNullOrWhiteSpace(dto.IdempotencyKey) ||
                        dto.CheckOut <= dto.CheckIn)
                    {
                        _logger.LogError(
                            "Invalid reservation event at {Offset}.",
                            result.TopicPartitionOffset);

                        var sentToDlq = await SendToDlqAsync(
                            dlqProducer!,
                            dlqTopic,
                            result.Message.Value,
                            result.TopicPartitionOffset,
                            stoppingToken);

                        if (sentToDlq)
                        {
                            consumer.Commit(result);
                        }

                        continue;
                    }

                    using var scope = _serviceProvider.CreateScope();

                    var context = scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                    await using var transaction =
                        await context.Database.BeginTransactionAsync(
                            stoppingToken);

                    var idem = await context.Idempotencies
                        .FirstOrDefaultAsync(
                            x => x.Key == dto.IdempotencyKey,
                            stoppingToken);

                    if (idem == null)
                    {
                        throw new InvalidOperationException(
                            "Idempotency record was not found.");
                    }

                    // A previously handled event must not create another
                    // reservation when Kafka redelivers it.
                    if (idem.Status is "COMPLETED" or "FAILED")
                    {
                        await transaction.CommitAsync(stoppingToken);
                        consumer.Commit(result);
                        continue;
                    }

                    var connection = context.Database.GetDbConnection();

                    await using var command = connection.CreateCommand();
                    command.Transaction = context.Database
                        .CurrentTransaction!
                        .GetDbTransaction();

                    command.CommandText = """
                        DECLARE @result int;

                        EXEC @result = sys.sp_getapplock
                            @Resource = @resource,
                            @LockMode = 'Exclusive',
                            @LockOwner = 'Transaction',
                            @LockTimeout = 10000;

                        SELECT @result;
                        """;

                    var resourceParameter = command.CreateParameter();
                    resourceParameter.ParameterName = "@resource";
                    resourceParameter.Value = $"HotelReservation:Room:{dto.RoomId}";
                    command.Parameters.Add(resourceParameter);

                    var lockResultObject = await command.ExecuteScalarAsync(stoppingToken);
                    var lockResult = Convert.ToInt32(lockResultObject);

                    if (lockResult < 0)
                    {
                        throw new InvalidOperationException(
                            $"Could not acquire room lock. SQL result: {lockResult}");
                    }

                    // Check overlapping reservations after acquiring the lock.
                    var hasConflict = await context.Reservations.AnyAsync(x =>
                        x.RoomId == dto.RoomId &&
                        x.CheckInDate < dto.CheckOut &&
                        x.CheckOutDate > dto.CheckIn,
                        stoppingToken);

                    if (hasConflict)
                    {
                        idem.Status = "FAILED";
                        idem.Response = JsonSerializer.Serialize(new
                        {
                            code = "ROOM_CONFLICT",
                            message = "The room is unavailable for the requested dates.",
                            roomId = dto.RoomId,
                            checkIn = dto.CheckIn,
                            checkOut = dto.CheckOut
                        });

                        await context.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);

                        // The conflict result is now durably recorded.
                        consumer.Commit(result);

                        _logger.LogWarning(
                            "Reservation rejected due to room conflict. Key: {Key}",
                            dto.IdempotencyKey);

                        continue;
                    }

                    context.Reservations.Add(new Reservation
                    {
                        RoomId = dto.RoomId,
                        UserId = dto.UserId,
                        CheckInDate = dto.CheckIn,
                        CheckOutDate = dto.CheckOut,
                        CreatedAt = DateTime.UtcNow
                    });

                    idem.Status = "COMPLETED";
                    idem.Response = JsonSerializer.Serialize(new
                    {
                        message = "Reservation created successfully",
                        roomId = dto.RoomId,
                        userId = dto.UserId,
                        checkIn = dto.CheckIn,
                        checkOut = dto.CheckOut
                    });

                    await context.SaveChangesAsync(stoppingToken);
                    await transaction.CommitAsync(stoppingToken);

                    // Commit Kafka offset only after the database commit.
                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Reservation completed. Key: {Key}",
                        dto.IdempotencyKey);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consumption error.");
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing reservation message. Kafka offset: {Offset}",
                        result?.TopicPartitionOffset);
                }
            }
        }
        finally
        {
            consumer.Close();
            dlqProducer?.Dispose();

            _logger.LogInformation("Reservation consumer stopped.");
        }
    }
}
