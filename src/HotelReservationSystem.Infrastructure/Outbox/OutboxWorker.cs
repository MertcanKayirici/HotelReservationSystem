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
        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");
        var bootstrap = _config["Kafka:BootstrapServers"];
        var topic = _config["Kafka:Topic"];

        KafkaProducer producer = null;

        if (kafkaEnabled)
        {
            producer = new KafkaProducer(bootstrap);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var messages = await context.OutboxMessages
                .Where(x => !x.Processed)
                .ToListAsync();

            foreach (var msg in messages)
            {
                try
                {
                    if (kafkaEnabled)
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
                    Console.WriteLine("Kafka error: " + ex.Message);
                }
            }

            await context.SaveChangesAsync();

            await Task.Delay(2000, stoppingToken);
        }
    }
}