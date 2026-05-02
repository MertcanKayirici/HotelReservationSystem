using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    /// <summary>
    /// Defines a unit of work abstraction to ensure transactional consistency
    /// across multiple operations within a single execution scope.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Executes the provided asynchronous action within a transactional boundary.
        /// Ensures that all operations inside the action are committed or rolled back as a unit.
        /// </summary>
        /// <param name="action">Asynchronous operation to execute.</param>
        Task ExecuteAsync(Func<Task> action);
    }
}