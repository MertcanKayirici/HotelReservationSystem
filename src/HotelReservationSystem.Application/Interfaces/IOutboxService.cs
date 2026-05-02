using HotelReservationSystem.Domain.Entities;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    /// <summary>
    /// Defines operations for persisting outbox messages.
    /// Used to implement the Outbox Pattern for reliable event publishing.
    /// </summary>
    public interface IOutboxService
    {
        /// <summary>
        /// Adds a new message to the outbox store.
        /// Messages are later processed asynchronously by a background worker.
        /// </summary>
        /// <param name="message">Outbox message entity to be persisted.</param>
        Task AddAsync(OutboxMessage message);
    }
}