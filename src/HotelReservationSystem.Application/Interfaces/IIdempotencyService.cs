using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    using HotelReservationSystem.Domain.Entities;

    /// <summary>
    /// Provides operations for managing idempotent request handling.
    /// Ensures that duplicate requests with the same key are processed safely and consistently.
    /// </summary>
    public interface IIdempotencyService
    {
        /// <summary>
        /// Updates the status and stored response associated with a given idempotency key.
        /// Typically used when a request completes or changes state.
        /// </summary>
        /// <param name="key">Unique idempotency key.</param>
        /// <param name="status">Current processing status (e.g., IN_PROGRESS, COMPLETED, FAILED).</param>
        /// <param name="response">Serialized response payload to persist.</param>
        Task UpdateStatusAsync(string key, string status, string response);

        /// <summary>
        /// Retrieves the idempotency record associated with the given key.
        /// Used to determine if a request has already been processed.
        /// </summary>
        /// <param name="key">Unique idempotency key.</param>
        /// <returns>Idempotency entity if found; otherwise null.</returns>
        Task<Idempotency> GetAsync(string key);

        /// <summary>
        /// Creates a new idempotency record for an incoming request.
        /// Typically invoked at the start of request processing.
        /// </summary>
        /// <param name="entity">Idempotency entity to persist.</param>
        Task CreateAsync(Idempotency entity);
    }
}