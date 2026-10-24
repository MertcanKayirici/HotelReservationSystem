namespace HotelReservationSystem.Infrastructure.Kafka;

public class KafkaProducerFactory : IKafkaProducerFactory
{
    public IKafkaProducer Create(string bootstrapServers)
    {
        return new KafkaProducer(bootstrapServers);
    }
}
