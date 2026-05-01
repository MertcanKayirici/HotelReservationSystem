using Confluent.Kafka;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        var kafkaEnabled = _config.GetValue<bool>("Kafka:Enabled");

        if (!kafkaEnabled)
        {
            Console.WriteLine("⚠️ Kafka OFF → Consumer çalışmaz (fallback mode)");
            return;
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = "reservation-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        consumer.Subscribe(_config["Kafka:Topic"]);

        Console.WriteLine("🟢 CONSUMER STARTED");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                Console.WriteLine($"📥 EVENT: {result.Message.Value}");

                var dto = JsonSerializer.Deserialize<CreateReservationDto>(result.Message.Value);

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Reservations.AddAsync(new Reservation
                {
                    RoomId = dto.RoomId,
                    UserId = dto.UserId,
                    CheckInDate = dto.CheckIn,
                    CheckOutDate = dto.CheckOut
                });

                await context.SaveChangesAsync();

                Console.WriteLine("✅ DB WRITE (Consumer)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ CONSUMER ERROR: " + ex.Message);
            }
        }
    }
}