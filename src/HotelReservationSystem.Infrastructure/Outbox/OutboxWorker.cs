using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Initializes the worker with required dependencies.
    /// </summary>
    public OutboxWorker(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<OutboxWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Main execution loop for processing outbox messages.
    /// Continuously polls the database and attempts message delivery.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Worker Started");

        // Read Kafka configuration
        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");
        var bootstrap = _config["Kafka:BootstrapServers"];
        var topic = _config["Kafka:Topic"];
        var dlqTopic = _config["Kafka:DlqTopic"];

        _logger.LogInformation("KafkaEnabled: {KafkaEnabled}", kafkaEnabled);
        _logger.LogInformation("Bootstrap: {Bootstrap}", bootstrap);

        KafkaProducer? producer = null;

        // Initialize Kafka producer if enabled
        if (kafkaEnabled && !string.IsNullOrWhiteSpace(bootstrap))
        {
            try
            {
                producer = new KafkaProducer(bootstrap);
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

                // Fetch unprocessed and non-dead messages (batch size: 50)
                var messages = await context.OutboxMessages
                    .Where(x => !x.Processed && !x.Dead)
                    .OrderBy(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                _logger.LogInformation("Found {Count} messages", messages.Count);

                foreach (var msg in messages)
                {
                    try
                    {
                        _logger.LogInformation("Processing message {Id}", msg.Id);
                        _logger.LogInformation("Payload: {Payload}", msg.Payload);

                        // Send message to Kafka if enabled
                        if (kafkaEnabled && producer != null)
                        {
                            await producer.SendAsync(topic!, msg.Payload);
                            _logger.LogInformation("Kafka delivered");
                        }
                        else
                        {
                            // Fallback behavior when Kafka is disabled
                            _logger.LogWarning("Kafka OFF → Fake event");
                        }

                        // Mark message as successfully processed
                        msg.Processed = true;
                    }
                    catch (Exception ex)
                    {
                        // Increment retry count on failure
                        msg.RetryCount++;

                        _logger.LogWarning(ex, "Retry {RetryCount}", msg.RetryCount);

                        // Apply exponential backoff delay
                        var delaySeconds = Math.Pow(2, msg.RetryCount);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);

                        // Move message to dead-letter state after max retries
                        if (msg.RetryCount >= 5)
                        {
                            msg.Dead = true;

                            _logger.LogError("Moved to DLQ: {Payload}", msg.Payload);

                            // Attempt to send to DLQ topic if configured
                            if (kafkaEnabled && producer != null && !string.IsNullOrWhiteSpace(dlqTopic))
                            {
                                try
                                {
                                    await producer.SendAsync(dlqTopic, msg.Payload);
                                    _logger.LogInformation("Sent to DLQ");
                                }
                                catch (Exception dlqEx)
                                {
                                    _logger.LogError(dlqEx, "DLQ send failed");
                                }
                            }
                        }
                    }
                }

                // Persist all changes (processed flags, retry counts, dead status)
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Catch unexpected worker-level failures
                _logger.LogError(ex, "Worker crash");
            }

            // Delay between polling cycles
            await Task.Delay(2000, stoppingToken);
        }

        _logger.LogInformation("Outbox Worker Stopped");
    }
}