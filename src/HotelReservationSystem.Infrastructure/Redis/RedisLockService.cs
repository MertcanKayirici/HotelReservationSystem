using StackExchange.Redis;
using HotelReservationSystem.Application.Interfaces;

namespace HotelReservationSystem.Infrastructure.Redis
{
    public class RedisLockService : IRedisLockService
    {
        private readonly IDatabase? _db;

        public RedisLockService(IConnectionMultiplexer? redis)
        {
            _db = redis?.GetDatabase();

            Console.WriteLine(_db == null
                ? "❌ REDIS DB NULL (fallback mode)"
                : "✔ REDIS DB CONNECTED");
        }

        // 🔥 LOCK AL
        public async Task<(bool Acquired, string Token)> AcquireLockAsync(string key, TimeSpan expiry)
        {
            var token = Guid.NewGuid().ToString("N");

            Console.WriteLine($"LOCK TRY: {key}");

            // ❗ Redis yok → lock YOK
            if (_db == null)
            {
                Console.WriteLine("⚠ Redis disabled → NO LOCK");
                return (false, token);
            }

            try
            {
                var ok = await _db.StringSetAsync(
                    key,
                    token,
                    expiry,
                    When.NotExists
                );

                Console.WriteLine($"LOCK RESULT: {ok}");

                return (ok, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Redis lock error: " + ex.Message);
                return (false, token);
            }
        }

        // 🔥 LOCK BIRAK (SAFE LUA)
        public async Task ReleaseLockAsync(string key, string token)
        {
            if (_db == null)
                return;

            try
            {
                var script = @"
                    if redis.call('GET', KEYS[1]) == ARGV[1] then
                        return redis.call('DEL', KEYS[1])
                    else
                        return 0
                    end";

                await _db.ScriptEvaluateAsync(
                    script,
                    new RedisKey[] { key },
                    new RedisValue[] { token }
                );

                Console.WriteLine($"LOCK RELEASE: {key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Redis release error: " + ex.Message);
            }
        }
    }
}