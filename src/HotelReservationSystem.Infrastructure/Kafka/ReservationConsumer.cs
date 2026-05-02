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

/// <summary>
/// Background Kafka consumer responsible for processing reservation events.
/// Consumes messages, persists data, and updates idempotency state.
/// </summary>
public class ReservationConsumer : BackgroundService
{
    /// <summary>
    /// Service provider used to create scoped dependencies.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Application configuration source.
    /// </summary>
    private readonly IConfiguration _config;

    /// <summary>
    /// Logger for structured logging.
    /// </summary>
    private readonly ILogger<ReservationConsumer> _logger;

    /// <summary>
    /// Initializes the consumer with required dependencies.
    /// </summary>
    public ReservationConsumer(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<ReservationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Main execution loop for consuming Kafka messages.
    /// Runs continuously until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if Kafka integration is enabled
        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");

        if (!kafkaEnabled)
        {
            _logger.LogWarning("Kafka OFF → Consumer disabled");
            return;
        }

        // Configure Kafka consumer
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = "reservation-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        // Build and subscribe consumer to topic
        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(_config["Kafka:Topic"]);

        _logger.LogInformation("Consumer started");

        // Continuous consumption loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Consume message from Kafka
                var result = consumer.Consume(stoppingToken);

                _logger.LogInformation("Event received: {Payload}", result.Message.Value);

                // Deserialize incoming payload into DTO
                var dto = JsonSerializer.Deserialize<CreateReservationDto>(
                    result.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Validate DTO integrity
                if (dto == null || string.IsNullOrEmpty(dto.IdempotencyKey))
                {
                    _logger.LogError("Invalid DTO");
                    continue;
                }

                // Log tracing identifiers
                _logger.LogInformation("IdempotencyKey: {Key}", dto.IdempotencyKey);
                _logger.LogInformation("CorrelationId: {CorrelationId}", dto.CorrelationId);

                // Create scoped service provider for DbContext
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Check for duplicate reservation entry to prevent double inserts
                var exists = await context.Reservations.AnyAsync(x =>
                    x.RoomId == dto.RoomId &&
                    x.UserId == dto.UserId &&
                    x.CheckInDate == dto.CheckIn &&
                    x.CheckOutDate == dto.CheckOut
                );

                if (exists)
                {
                    _logger.LogWarning("Duplicate event skipped");

                    // Commit offset to avoid re-processing
                    consumer.Commit(result);
                    continue;
                }

                // Persist reservation entity
                await context.Reservations.AddAsync(new Reservation
                {
                    RoomId = dto.RoomId,
                    UserId = dto.UserId,
                    CheckInDate = dto.CheckIn,
                    CheckOutDate = dto.CheckOut
                });

                await context.SaveChangesAsync();

                // Update idempotency record after successful processing
                var idem = await context.Idempotencies
                    .FirstOrDefaultAsync(x => x.Key == dto.IdempotencyKey);

                if (idem != null)
                {
                    idem.Status = "COMPLETED";
                    idem.Response = "Reservation created successfully";

                    await context.SaveChangesAsync();
                }

                _logger.LogInformation("DB write successful");

                // Commit offset after successful processing
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                // Handle Kafka-specific errors
                _logger.LogError(ex, "Kafka error");
            }
            catch (Exception ex)
            {
                // Handle general processing errors
                _logger.LogError(ex, "Processing error");
            }
        }

        // Gracefully close consumer on shutdown
        consumer.Close();
    }
}