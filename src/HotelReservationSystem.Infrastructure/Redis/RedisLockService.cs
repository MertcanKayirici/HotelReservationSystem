using HotelReservationSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

/// <summary>
/// Implements distributed locking using Redis.
/// Provides a fallback mechanism when Redis is not available.
/// </summary>
public class RedisLockService : IRedisLockService
{
    /// <summary>
    /// Redis connection multiplexer instance.
    /// May be null if Redis is disabled or not configured.
    /// </summary>
    private readonly IConnectionMultiplexer? _redis;

    /// <summary>
    /// Initializes the service and attempts to resolve Redis dependency.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public RedisLockService(IServiceProvider serviceProvider)
    {
        // Attempt to resolve Redis connection (optional dependency)
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
    }

    /// <summary>
    /// Attempts to acquire a distributed lock using Redis.
    /// Falls back to a no-op lock if Redis is unavailable.
    /// </summary>
    /// <param name="key">Lock key.</param>
    /// <param name="expiry">Lock expiration duration.</param>
    /// <returns>
    /// Tuple containing:
    /// - bool: Indicates whether the lock was acquired
    /// - string: Lock token for ownership validation
    /// </returns>
    public async Task<(bool, string)> AcquireLockAsync(string key, TimeSpan expiry)
    {
        // Fallback behavior when Redis is disabled
        if (_redis == null)
        {
            Console.WriteLine("FAKE LOCK (Redis OFF)");

            // Always succeed with a generated token
            return (true, Guid.NewGuid().ToString());
        }

        // Get Redis database instance
        var db = _redis.GetDatabase();

        // Generate unique token for lock ownership
        var token = Guid.NewGuid().ToString();

        // Attempt to set lock key only if it does not exist
        var acquired = await db.StringSetAsync(key, token, expiry, When.NotExists);

        return (acquired, token);
    }

    /// <summary>
    /// Releases a previously acquired distributed lock.
    /// Ensures only the owner of the lock can release it.
    /// </summary>
    /// <param name="key">Lock key.</param>
    /// <param name="token">Lock ownership token.</param>
    public async Task ReleaseLockAsync(string key, string token)
    {
        // No-op if Redis is unavailable
        if (_redis == null)
            return;

        // Get Redis database instance
        var db = _redis.GetDatabase();

        // Retrieve current token stored in Redis
        var current = await db.StringGetAsync(key);

        // Only delete key if token matches (ensures safe unlock)
        if (current == token)
        {
            await db.KeyDeleteAsync(key);
        }
    }
}