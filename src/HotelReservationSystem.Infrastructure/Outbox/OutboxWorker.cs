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
                    .ToListAsync();

                foreach (var msg in messages)
                {
                    try
                    {
                        if (producer != null && kafkaEnabled)
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
                            Console.WriteLine("💀 DEAD LETTER");
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 WORKER LOOP CRASH: " + ex.Message);
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}