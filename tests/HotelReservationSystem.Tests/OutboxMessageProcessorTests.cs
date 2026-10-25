
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Kafka;
using HotelReservationSystem.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests;

public class OutboxMessageProcessorTests
{
    private readonly Mock<IKafkaProducer> _producer = new();
    private readonly Mock<ILogger<OutboxMessageProcessor>> _logger = new();

    private static OutboxMessage CreateMessage(int retryCount = 0)
    {
        return new OutboxMessage
        {
            Id = 1,
            Type = "ReservationCreated",
            Payload = """{"reservationId":1}""",
            CreatedAt = DateTime.UtcNow,
            RetryCount = retryCount,
            Processed = false,
            Dead = false
        };
    }

    [Fact]
    public async Task ProcessAsync_MarksMessageProcessed_WhenPublishSucceeds()
    {
        var message = CreateMessage();

        _producer
            .Setup(p => p.SendAsync(
                "reservations",
                message.Payload,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            "reservations",
            "reservations-dlq");

        await processor.ProcessAsync(message);

        Assert.True(message.Processed);
        Assert.Null(message.NextRetryAt);
        Assert.False(message.Dead);
    }

    [Fact]
    public async Task ProcessAsync_IncrementsRetryCount_WhenPublishFails()
    {
        var message = CreateMessage();

        _producer
            .Setup(p => p.SendAsync(
                "reservations",
                message.Payload,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kafka unavailable"));

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            "reservations",
            "reservations-dlq");

        await processor.ProcessAsync(message);

        Assert.False(message.Processed);
        Assert.Equal(1, message.RetryCount);
        Assert.NotNull(message.NextRetryAt);
    }

    [Fact]
    public async Task ProcessAsync_DefersMessage_WhenMainTopicIsMissing()
    {
        var message = CreateMessage();

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            null,
            "reservations-dlq");

        await processor.ProcessAsync(message);

        Assert.False(message.Processed);
        Assert.Equal(0, message.RetryCount);
        Assert.NotNull(message.NextRetryAt);

        _producer.Verify(
            p => p.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_SendsToDlq_WhenRetryLimitIsReached()
    {
        var message = CreateMessage(OutboxRetryPolicy.MaxRetries);

        _producer
            .Setup(p => p.SendAsync(
                "reservations-dlq",
                message.Payload,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            "reservations",
            "reservations-dlq");

        await processor.ProcessAsync(message);

        Assert.True(message.Dead);
        Assert.False(message.Processed);
        Assert.Null(message.NextRetryAt);

        _producer.Verify(
            p => p.SendAsync(
                "reservations-dlq",
                message.Payload,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _producer.Verify(
            p => p.SendAsync(
                "reservations",
                message.Payload,
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_DefersMessage_WhenDlqTopicIsMissing()
    {
        var message = CreateMessage(OutboxRetryPolicy.MaxRetries);

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            "reservations",
            null);

        await processor.ProcessAsync(message);

        Assert.False(message.Dead);
        Assert.False(message.Processed);
        Assert.NotNull(message.NextRetryAt);

        _producer.Verify(
            p => p.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_DefersMessage_WhenDlqPublishFails()
    {
        var message = CreateMessage(OutboxRetryPolicy.MaxRetries);

        _producer
            .Setup(p => p.SendAsync(
                "reservations-dlq",
                message.Payload,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DLQ unavailable"));

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            "reservations",
            "reservations-dlq");

        await processor.ProcessAsync(message);

        Assert.False(message.Dead);
        Assert.False(message.Processed);
        Assert.NotNull(message.NextRetryAt);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotIncrementRetryCount_WhenCancellationIsRequested()
    {
        var message = CreateMessage();
        using var cts = new CancellationTokenSource();

        _producer
            .Setup(p => p.SendAsync(
                "reservations",
                message.Payload,
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            });

        var processor = new OutboxMessageProcessor(
            _producer.Object,
            _logger.Object,
            "reservations",
            "reservations-dlq");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => processor.ProcessAsync(message, cts.Token));

        Assert.Equal(0, message.RetryCount);
        Assert.False(message.Processed);
    }

}
