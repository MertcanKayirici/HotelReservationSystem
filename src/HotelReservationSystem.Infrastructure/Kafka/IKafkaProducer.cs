namespace HotelReservationSystem.Infrastructure.Kafka;

public interface IKafkaProducer : IDisposable
{
    Task SendAsync(
        string topic,
        string message,
        CancellationToken cancellationToken = default);
}
