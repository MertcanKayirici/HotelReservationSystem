using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelReservationSystem.Infrastructure.Outbox;

/// <summary>
/// Background worker responsible for processing outbox messages
/// and publishing them to Kafka (or handling fallback scenarios).
/// Implements the Outbox Pattern for reliable event delivery.
/// </summary>
public class OutboxWorker : BackgroundService
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
    private readonly ILogger<OutboxWorker> _logger;

    private readonly IKafkaProducerFactory _producerFactory;

    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes the worker with required dependencies.
    /// </summary>
    public OutboxWorker(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<OutboxWorker> logger,
        IKafkaProducerFactory producerFactory,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
        _producerFactory = producerFactory;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Main execution loop for processing outbox messages.
    /// Continuously polls the database and attempts message delivery.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IKafkaProducer? producer = null;

        try
        {
            _logger.LogInformation("Outbox Worker Started");

            // Read Kafka configuration
            var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");
            var bootstrap = _config["Kafka:BootstrapServers"];
            var topic = _config["Kafka:Topic"];
            var dlqTopic = _config["Kafka:DlqTopic"];

            _logger.LogInformation("KafkaEnabled: {KafkaEnabled}", kafkaEnabled);
            _logger.LogInformation("Bootstrap: {Bootstrap}", bootstrap);

            // Initialize Kafka producer if enabled
            if (kafkaEnabled && !string.IsNullOrWhiteSpace(bootstrap))
            {
                try
                {
                    producer = _producerFactory.Create(bootstrap);
                    _logger.LogInformation("Kafka initialized");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka init failed");

                    // Disable Kafka if initialization fails
                    kafkaEnabled = false;
                }
            }

            // Continuous processing loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create scoped DbContext
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;

                    var messages = await context.OutboxMessages
                        .Where(x => !x.Processed
                            && !x.Dead
                            && (x.NextRetryAt == null || x.NextRetryAt <= now))
                        .OrderBy(x => x.CreatedAt)
                        .Take(50)
                        .ToListAsync(stoppingToken);

                    _logger.LogInformation("Found {Count} messages", messages.Count);

                    foreach (var msg in messages)
                    {
                        _logger.LogInformation(
                            "Processing outbox message {MessageId} of type {MessageType}",
                            msg.Id,
                            msg.Type);

                        if (!kafkaEnabled || producer == null || string.IsNullOrWhiteSpace(topic))
                        {
                            msg.NextRetryAt = DateTime.UtcNow.AddMinutes(1);

                            _logger.LogWarning(
                                "Kafka unavailable. Outbox message {MessageId} will retry at {NextRetryAt}.",
                                msg.Id,
                                msg.NextRetryAt);

                            continue;
                        }

                        var processor = new OutboxMessageProcessor(
                            producer,
                            _loggerFactory.CreateLogger<OutboxMessageProcessor>(),
                            topic,
                            dlqTopic);

                        await processor.ProcessAsync(msg, stoppingToken);

                        if (msg.Processed)
                        {
                            _logger.LogInformation(
                                "Outbox message {MessageId} delivered to Kafka.",
                                msg.Id);
                        }
                    }

                    // Persist all changes (processed flags, retry counts, dead status)
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Catch unexpected worker-level failures
                    _logger.LogError(ex, "Worker crash");
                }

                // Delay between polling cycles
                try
                {
                    await Task.Delay(2000, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            producer?.Dispose();
            _logger.LogInformation("Outbox Worker Stopped");
        }
    }
}