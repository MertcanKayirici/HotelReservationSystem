using Confluent.Kafka;

namespace HotelReservationSystem.Infrastructure.Kafka
{
    /// <summary>
    /// Kafka producer responsible for publishing messages to specified topics.
    /// Encapsulates Confluent.Kafka producer implementation.
    /// </summary>
    public class KafkaProducer : IDisposable
    {
        /// <summary>
        /// Underlying Kafka producer instance.
        /// </summary>
        private readonly IProducer<Null, string> _producer;

        /// <summary>
        /// Initializes the Kafka producer with bootstrap server configuration.
        /// </summary>
        /// <param name="bootstrapServers">Kafka broker address(es).</param>
        public KafkaProducer(string bootstrapServers)
        {
            // Configure Kafka producer settings
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            // Build producer instance
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        /// <summary>
        /// Sends a message asynchronously to the specified Kafka topic.
        /// </summary>
        /// <param name="topic">Target Kafka topic.</param>
        /// <param name="message">Message payload (serialized string).</param>
        public async Task SendAsync(string topic, string message)
        {
            // Produce message to Kafka topic
            var result = await _producer.ProduceAsync(topic, new Message<Null, string>
            {
                Value = message
            });

            // Log delivery metadata for observability
            Console.WriteLine($"Kafka delivered → {result.TopicPartitionOffset}");
        }

        /// <summary>
        /// Flushes pending messages and disposes the producer instance.
        /// Ensures graceful shutdown.
        /// </summary>
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}