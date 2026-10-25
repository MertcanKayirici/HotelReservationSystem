
using HotelReservationSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

public class RedisLockService : IRedisLockService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly bool _isDevelopment;

    public RedisLockService(
        IServiceProvider serviceProvider,
        IHostEnvironment environment)
    {
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task<(bool Acquired, string Token)> AcquireLockAsync(
        string key,
        TimeSpan expiry)
    {
        if (_redis == null)
        {
            if (!_isDevelopment)
            {
                throw new InvalidOperationException(
                    "Redis is required for distributed locking outside Development.");
            }

            Console.WriteLine("WARNING: Redis disabled; using development-only fake lock.");
            return (true, Guid.NewGuid().ToString());
        }

        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString();

        var acquired = await db.StringSetAsync(
            key, token, expiry, When.NotExists);

        return (acquired, token);
    }

    public async Task ReleaseLockAsync(string key, string token)
    {
        if (_redis == null)
            return;

        var db = _redis.GetDatabase();
        var current = await db.StringGetAsync(key);

        if (current == token)
            await db.KeyDeleteAsync(key);
    }
}
