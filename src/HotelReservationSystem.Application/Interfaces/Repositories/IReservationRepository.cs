using HotelReservationSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces.Repositories
{
    /// <summary>
    /// Defines data access operations related to reservation persistence.
    /// Abstracts database interactions for reservation entities.
    /// </summary>
    public interface IReservationRepository
    {
        /// <summary>
        /// Checks whether there is an existing reservation conflict for a given room and date range.
        /// Typically used before creating a new reservation.
        /// </summary>
        /// <param name="roomId">Target room identifier.</param>
        /// <param name="checkIn">Requested check-in date.</param>
        /// <param name="checkOut">Requested check-out date.</param>
        /// <returns>True if a conflicting reservation exists; otherwise false.</returns>
        Task<bool> ExistsConflictAsync(int roomId, DateTime checkIn, DateTime checkOut);

        /// <summary>
        /// Persists a new reservation entity to the data store.
        /// </summary>
        /// <param name="reservation">Reservation entity to be added.</param>
        Task AddAsync(Reservation reservation);

        /// <summary>
        /// Checks for reservation conflicts, potentially with different logic
        /// or use-case context than ExistsConflictAsync.
        /// </summary>
        /// <param name="roomId">Target room identifier.</param>
        /// <param name="checkIn">Requested check-in date.</param>
        /// <param name="checkOut">Requested check-out date.</param>
        /// <returns>True if a conflict exists; otherwise false.</returns>
        Task<bool> HasConflictAsync(int roomId, DateTime checkIn, DateTime checkOut);
    }
}