
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests;

public class OutboxWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenKafkaDisabled_DefersPendingMessage()
    {
        var databaseName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using var db = new AppDbContext(options);

        var message = new OutboxMessage
        {
            Id = 1,
            Type = "ReservationCreated",
            Payload = """{"reservationId":1}""",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Processed = false,
            Dead = false
        };

        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        var serviceProvider = services.BuildServiceProvider();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:Enabled"] = "false",
                ["Kafka:BootstrapServers"] = "",
                ["Kafka:Topic"] = "reservations",
                ["Kafka:DlqTopic"] = "reservations-dlq"
            })
            .Build();

        var producerFactory = new Mock<IKafkaProducerFactory>();
        var logger = Mock.Of<ILogger<OutboxWorker>>();
        var loggerFactory = LoggerFactory.Create(_ => { });

        var worker = new OutboxWorker(
            serviceProvider,
            config,
            logger,
            producerFactory.Object,
            loggerFactory);

        using var cts = new CancellationTokenSource();

        try
        {
            await worker.StartAsync(cts.Token);

            // Wait for the worker's first polling cycle.
            await Task.Delay(500);

            await cts.CancelAsync();
            await worker.StopAsync(CancellationToken.None);

            await using var verifyDb = new AppDbContext(options);

            var savedMessage = await verifyDb.OutboxMessages
                .AsNoTracking()
                .SingleAsync(x => x.Id == 1);

            Assert.False(savedMessage.Processed);
            Assert.False(savedMessage.Dead);
            Assert.NotNull(savedMessage.NextRetryAt);
            Assert.True(
                savedMessage.NextRetryAt.Value > DateTime.UtcNow,
                "The message should be deferred to a future retry.");

            producerFactory.Verify(
                f => f.Create(It.IsAny<string>()),
                Times.Never);
        }
        finally
        {
            worker.Dispose();
            serviceProvider.Dispose();
            loggerFactory.Dispose();
        }
    }
}
