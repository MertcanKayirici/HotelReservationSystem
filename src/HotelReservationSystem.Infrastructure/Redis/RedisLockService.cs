using HotelReservationSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public class RedisLockService : IRedisLockService
{
    private readonly IConnectionMultiplexer? _redis;

    public RedisLockService(IServiceProvider serviceProvider)
    {
        // 🔥 OPTIONAL RESOLVE
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
    }

    public async Task<(bool, string)> AcquireLockAsync(string key, TimeSpan expiry)
    {
        // 🔥 REDIS YOK → FAKE LOCK
        if (_redis == null)
        {
            Console.WriteLine("⚠️ FAKE LOCK (Redis OFF)");
            return (true, Guid.NewGuid().ToString());
        }

        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString();

        var acquired = await db.StringSetAsync(key, token, expiry, When.NotExists);

        return (acquired, token);
    }

    public async Task ReleaseLockAsync(string key, string token)
    {
        if (_redis == null)
            return;

        var db = _redis.GetDatabase();

        var current = await db.StringGetAsync(key);

        if (current == token)
        {
            await db.KeyDeleteAsync(key);
        }
    }
}