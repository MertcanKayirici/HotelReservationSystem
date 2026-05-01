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

        KafkaProducer? producer = null;

        if (kafkaEnabled)
        {
            try
            {
                producer = new KafkaProducer(bootstrap);
                Console.WriteLine("Kafka initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Kafka INIT FAIL: " + ex.Message);
                kafkaEnabled = false; // 🔥 fallback
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
                    .OrderBy(x => x.CreatedAt) // 🔥 önemli
                    .Take(50) // 🔥 batch limit (prod için şart)
                    .ToListAsync(stoppingToken);

                foreach (var msg in messages)
                {
                    try
                    {
                        if (kafkaEnabled && producer != null)
                        {
                            await producer.SendAsync(topic, msg.Payload);
                        }
                        else
                        {
                            Console.WriteLine($"[FAKE EVENT] {msg.Payload}");
                        }

                        msg.Processed = true;
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount++;

                        Console.WriteLine($"Retry {msg.RetryCount}: {ex.Message}");

                        if (msg.RetryCount >= 5)
                        {
                            msg.Dead = true;
                            Console.WriteLine($"💀 DEAD LETTER: {msg.Payload}");
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 WORKER LOOP CRASH: " + ex.Message);
            }

            try
            {
                await Task.Delay(2000, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // graceful shutdown (優雅終了 - düzgün kapanış)
            }
        }

        Console.WriteLine("OUTBOX WORKER STOPPED");
    }
}