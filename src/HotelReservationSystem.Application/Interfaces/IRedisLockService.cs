using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    /// <summary>
    /// Provides distributed locking capabilities using Redis.
    /// Used to prevent concurrent operations on shared resources (e.g., same room reservation).
    /// </summary>
    public interface IRedisLockService
    {
        /// <summary>
        /// Attempts to acquire a distributed lock for a given key with a specified expiration time.
        /// </summary>
        /// <param name="key">Unique identifier for the lock (e.g., resource key).</param>
        /// <param name="expiry">Lock expiration duration to avoid deadlocks.</param>
        /// <returns>
        /// Tuple containing:
        /// - Acquired: Indicates whether the lock was successfully obtained.
        /// - Token: Unique token required for releasing the lock safely.
        /// </returns>
        Task<(bool Acquired, string Token)> AcquireLockAsync(string key, TimeSpan expiry);

        /// <summary>
        /// Releases a previously acquired distributed lock using its token.
        /// Ensures that only the lock owner can release it.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="token">Lock ownership token.</param>
        Task ReleaseLockAsync(string key, string token);
    }
}