using StackExchange.Redis;
using HotelReservationSystem.Application.Interfaces;

namespace HotelReservationSystem.Infrastructure.Redis
{
    public class RedisLockService : IRedisLockService
    {
        private readonly IDatabase? _db;

        public RedisLockService(IConnectionMultiplexer redis)
        {
            _db = redis?.GetDatabase();
        }

        // 🔥 Lock al (token ile)
        public async Task<(bool Acquired, string Token)> AcquireLockAsync(string key, TimeSpan expiry)
        {
            var token = Guid.NewGuid().ToString("N");

            try
            {
                if (_db == null)
                    return (true, token); // DEV MODE

                var ok = await _db.StringSetAsync(key, token, expiry, When.NotExists);
                return (ok, token);
            }
            catch
            {
                return (true, token); // 🔥 fallback: sistemi kırma
            }
        }

        // 🔥 Sadece kendi lock’ını bırak (token kontrolü)
        public async Task ReleaseLockAsync(string key, string token)
        {
            try
            {
                if (_db == null)
                    return;

                // Lua ile atomik kontrol + sil
                var script = @"
                        if redis.call('GET', KEYS[1]) == ARGV[1] then
                            return redis.call('DEL', KEYS[1])
                        else
                            return 0
                        end";

                await _db.ScriptEvaluateAsync(script,
                    new RedisKey[] { key },
                    new RedisValue[] { token });
            }
            catch
            {
                // ignore
            }
        }
    }
}