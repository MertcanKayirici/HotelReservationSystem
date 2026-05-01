using Confluent.Kafka;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class ReservationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public ReservationConsumer(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue<bool>("Kafka:Enabled");

        if (!enabled)
        {
            Console.WriteLine("🚫 Kafka Consumer DISABLED (safe mode)");
            return;
        }

        var bootstrap = _config["Kafka:BootstrapServers"];
        var topic = _config["Kafka:Topic"];

        var configConsumer = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "reservation-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(configConsumer).Build();
        consumer.Subscribe(topic);

        Console.WriteLine("🎧 Kafka Consumer STARTED");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                Console.WriteLine($"📥 EVENT RECEIVED: {result.Message.Value}");

                using var scope = _serviceProvider.CreateScope();
                var coordinator = scope.ServiceProvider.GetRequiredService<IReservationCoordinator>();

                var dto = JsonSerializer.Deserialize<CreateReservationDto>(result.Message.Value);

                if (dto != null)
                {
                    await coordinator.HandleAsync(dto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Consumer error: " + ex.Message);
            }
        }
    }
}