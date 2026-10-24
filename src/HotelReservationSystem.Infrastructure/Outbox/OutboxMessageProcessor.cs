
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;

namespace HotelReservationSystem.Infrastructure.Outbox;

public sealed class OutboxMessageProcessor
{
    private readonly IKafkaProducer _producer;
    private readonly ILogger<OutboxMessageProcessor> _logger;
    private readonly string? _topic;
    private readonly string? _dlqTopic;

    public OutboxMessageProcessor(
        IKafkaProducer producer,
        ILogger<OutboxMessageProcessor> logger,
        string? topic,
        string? dlqTopic)
    {
        _producer = producer;
        _logger = logger;
        _topic = topic;
        _dlqTopic = dlqTopic;
    }

    public async Task ProcessAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_topic))
        {
            message.NextRetryAt = DateTime.UtcNow.AddMinutes(1);
            return;
        }

        if (message.RetryCount >= OutboxRetryPolicy.MaxRetries)
        {
            await SendToDeadLetterQueueAsync(message, cancellationToken);
            return;
        }

        try
        {
            await _producer.SendAsync(_topic, message.Payload, cancellationToken);

            message.Processed = true;
            message.NextRetryAt = null;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            message.RetryCount++;

            _logger.LogWarning(
                ex,
                "Outbox message {MessageId} failed. Retry count: {RetryCount}",
                message.Id,
                message.RetryCount);

            message.NextRetryAt =
                message.RetryCount >= OutboxRetryPolicy.MaxRetries
                    ? DateTime.UtcNow
                    : OutboxRetryPolicy.GetNextRetryAt(
                        message.RetryCount,
                        DateTime.UtcNow);
        }
    }

    private async Task SendToDeadLetterQueueAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_dlqTopic))
        {
            message.NextRetryAt = DateTime.UtcNow.AddMinutes(5);
            return;
        }

        try
        {
            await _producer.SendAsync(
                _dlqTopic,
                message.Payload,
                cancellationToken);

            message.Dead = true;
            message.NextRetryAt = null;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DLQ send failed for message {MessageId}.",
                message.Id);

            message.NextRetryAt = DateTime.UtcNow.AddMinutes(5);
        }
    }
}
