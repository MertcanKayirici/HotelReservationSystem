using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public OutboxWorker(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("🟢 OUTBOX WORKER STARTED");

        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");
        var bootstrap = _config["Kafka:BootstrapServers"];
        var topic = _config["Kafka:Topic"];
        var dlqTopic = _config["Kafka:DlqTopic"];

        Console.WriteLine($"KafkaEnabled: {kafkaEnabled}");
        Console.WriteLine($"Bootstrap: {bootstrap}");
        Console.WriteLine($"Topic: {topic}");
        Console.WriteLine($"DLQ: {dlqTopic}");

        KafkaProducer? producer = null;

        if (kafkaEnabled && !string.IsNullOrWhiteSpace(bootstrap))
        {
            try
            {
                producer = new KafkaProducer(bootstrap);
                Console.WriteLine("✅ Kafka initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Kafka INIT FAIL: " + ex.Message);
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

                Console.WriteLine($"📦 Found {messages.Count} messages");

                foreach (var msg in messages)
                {
                    try
                    {
                        Console.WriteLine($"➡ Processing ID: {msg.Id}");

                        if (kafkaEnabled && producer != null)
                        {
                            await producer.SendAsync(topic!, msg.Payload);
                            Console.WriteLine("✅ Kafka delivered");
                        }
                        else
                        {
                            Console.WriteLine($"🧪 FAKE EVENT → {msg.Payload}");
                        }

                        msg.Processed = true;
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount++;

                        Console.WriteLine($"⚠ Retry {msg.RetryCount}: {ex.Message}");

                        var delaySeconds = Math.Pow(2, msg.RetryCount);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);

                        if (msg.RetryCount >= 5)
                        {
                            msg.Dead = true;

                            Console.WriteLine($"💀 DLQ → {msg.Payload}");

                            // 🔥 DLQ SEND
                            if (kafkaEnabled && producer != null && !string.IsNullOrWhiteSpace(dlqTopic))
                            {
                                try
                                {
                                    await producer.SendAsync(dlqTopic, msg.Payload);
                                    Console.WriteLine("📤 Sent to DLQ topic");
                                }
                                catch (Exception dlqEx)
                                {
                                    Console.WriteLine("❌ DLQ SEND FAIL: " + dlqEx.Message);
                                }
                            }
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 WORKER CRASH: " + ex);
            }

            await Task.Delay(2000, stoppingToken);
        }

        Console.WriteLine("🛑 OUTBOX WORKER STOPPED");
    }
}