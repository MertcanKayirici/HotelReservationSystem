namespace HotelReservationSystem.Infrastructure.Kafka;

public interface IKafkaProducerFactory
{
    IKafkaProducer Create(string bootstrapServers);
}
