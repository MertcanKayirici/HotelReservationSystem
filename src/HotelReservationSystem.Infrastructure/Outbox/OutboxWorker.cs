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
        var bootstrap = _config.GetValue<string>("Kafka:BootstrapServers");
        var topic = _config.GetValue<string>("Kafka:Topic");

        Console.WriteLine($"KafkaEnabled: {kafkaEnabled}");
        Console.WriteLine($"Bootstrap: {bootstrap}");
        Console.WriteLine($"Topic: {topic}");

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
            Console.WriteLine("🔁 WORKER LOOP");

            try
            {
                using var scope = _serviceProvider.CreateScope();

                var context = scope.ServiceProvider.GetService<AppDbContext>();

                if (context == null)
                {
                    Console.WriteLine("❌ DbContext NULL");
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                var messages = await context.OutboxMessages
                    .Where(x => !x.Processed && !x.Dead)
                    .OrderBy(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                Console.WriteLine($"📦 Found {messages.Count} messages");

                if (!messages.Any())
                {
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                foreach (var msg in messages)
                {
                    try
                    {
                        Console.WriteLine($"👉 Processing ID: {msg.Id}");

                        if (kafkaEnabled && producer != null)
                        {
                            Console.WriteLine($"🚀 Kafka SEND → {msg.Payload}");

                            await producer.SendAsync(topic!, msg.Payload);

                            Console.WriteLine("✅ Kafka delivered");

                            msg.Processed = true;
                        }
                        else
                        {
                            Console.WriteLine($"🧪 FAKE EVENT → {msg.Payload}");

                            msg.Processed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount++;

                        Console.WriteLine($"⚠ Retry {msg.RetryCount}: {ex.Message}");

                        if (msg.RetryCount >= 5)
                        {
                            msg.Dead = true;
                            Console.WriteLine($"💀 DEAD LETTER → {msg.Payload}");
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 WORKER CRASH: " + ex.Message);
            }

            await Task.Delay(2000, stoppingToken);
        }

        Console.WriteLine("🛑 OUTBOX WORKER STOPPED");
    }
}