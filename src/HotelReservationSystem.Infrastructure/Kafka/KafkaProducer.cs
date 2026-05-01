using Confluent.Kafka;

namespace HotelReservationSystem.Infrastructure.Kafka
{
    public class KafkaProducer : IDisposable
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(string bootstrapServers)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendAsync(string topic, string message)
        {
            var result = await _producer.ProduceAsync(topic, new Message<Null, string>
            {
                Value = message
            });

            Console.WriteLine($"Kafka delivered → {result.TopicPartitionOffset}");
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}