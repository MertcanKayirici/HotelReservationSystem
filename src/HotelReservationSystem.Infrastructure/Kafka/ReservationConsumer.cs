using Confluent.Kafka;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class ReservationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<ReservationConsumer> _logger;

    public ReservationConsumer(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<ReservationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");

        if (!kafkaEnabled)
        {
            _logger.LogWarning("Kafka OFF → Consumer disabled");
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = "reservation-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(_config["Kafka:Topic"]);

        _logger.LogInformation("Consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                _logger.LogInformation("Event received: {Payload}", result.Message.Value);

                var dto = JsonSerializer.Deserialize<CreateReservationDto>(
                    result.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto == null || string.IsNullOrEmpty(dto.IdempotencyKey))
                {
                    _logger.LogError("Invalid DTO");
                    continue;
                }

                _logger.LogInformation("IdempotencyKey: {Key}", dto.IdempotencyKey);
                _logger.LogInformation("CorrelationId: {CorrelationId}", dto.CorrelationId);

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var exists = await context.Reservations.AnyAsync(x =>
                    x.RoomId == dto.RoomId &&
                    x.UserId == dto.UserId &&
                    x.CheckInDate == dto.CheckIn &&
                    x.CheckOutDate == dto.CheckOut
                );

                if (exists)
                {
                    _logger.LogWarning("Duplicate event skipped");
                    consumer.Commit(result);
                    continue;
                }

                await context.Reservations.AddAsync(new Reservation
                {
                    RoomId = dto.RoomId,
                    UserId = dto.UserId,
                    CheckInDate = dto.CheckIn,
                    CheckOutDate = dto.CheckOut
                });

                await context.SaveChangesAsync();

                var idem = await context.Idempotencies
                    .FirstOrDefaultAsync(x => x.Key == dto.IdempotencyKey);

                if (idem != null)
                {
                    idem.Status = "COMPLETED";
                    idem.Response = "Reservation created successfully";
                    await context.SaveChangesAsync();
                }

                _logger.LogInformation("DB write successful");

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing error");
            }
        }

        consumer.Close();
    }
}