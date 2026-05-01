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

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = "reservation-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // 🔥 manuel commit
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

        consumer.Subscribe(_config["Kafka:Topic"]);

        Console.WriteLine("🟢 CONSUMER STARTED");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                Console.WriteLine($"📥 EVENT: {result.Message.Value}");

                var dto = JsonSerializer.Deserialize<CreateReservationDto>(result.Message.Value);

                if (dto == null)
                {
                    Console.WriteLine("❌ DTO NULL → skip");
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 🔥 IDEMPOTENCY (duplicate event koruma)
                var exists = await context.Reservations.AnyAsync(x =>
                    x.RoomId == dto.RoomId &&
                    x.UserId == dto.UserId &&
                    x.CheckInDate == dto.CheckIn &&
                    x.CheckOutDate == dto.CheckOut
                );

                if (exists)
                {
                    Console.WriteLine("⚠ DUPLICATE EVENT SKIPPED");

                    consumer.Commit(result); // yine commit et
                    continue;
                }

                await context.Reservations.AddAsync(new Reservation
                {
                    RoomId = dto.RoomId,
                    UserId = dto.UserId,
                    CheckInDate = dto.CheckIn,
                    CheckOutDate = dto.CheckOut
                });

                await context.SaveChangesAsync();

                Console.WriteLine("✅ DB WRITE (Consumer)");

                // 🔥 sadece başarılıysa commit
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine("❌ KAFKA ERROR: " + ex.Error.Reason);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ PROCESS ERROR: " + ex.Message);
            }
        }

        consumer.Close();
    }
}