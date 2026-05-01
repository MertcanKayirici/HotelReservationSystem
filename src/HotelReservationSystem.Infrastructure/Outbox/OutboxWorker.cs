using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<OutboxWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Worker Started");

        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");
        var bootstrap = _config["Kafka:BootstrapServers"];
        var topic = _config["Kafka:Topic"];
        var dlqTopic = _config["Kafka:DlqTopic"];

        _logger.LogInformation("KafkaEnabled: {KafkaEnabled}", kafkaEnabled);
        _logger.LogInformation("Bootstrap: {Bootstrap}", bootstrap);

        KafkaProducer? producer = null;

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
                kafkaEnabled = false;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

                        if (kafkaEnabled && producer != null)
                        {
                            await producer.SendAsync(topic!, msg.Payload);
                            _logger.LogInformation("Kafka delivered");
                        }
                        else
                        {
                            _logger.LogWarning("Kafka OFF → Fake event");
                        }

                        msg.Processed = true;
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount++;

                        _logger.LogWarning(ex, "Retry {RetryCount}", msg.RetryCount);

                        var delaySeconds = Math.Pow(2, msg.RetryCount);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);

                        if (msg.RetryCount >= 5)
                        {
                            msg.Dead = true;

                            _logger.LogError("Moved to DLQ: {Payload}", msg.Payload);

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

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker crash");
            }

            await Task.Delay(2000, stoppingToken);
        }

        _logger.LogInformation("Outbox Worker Stopped");
    }
}