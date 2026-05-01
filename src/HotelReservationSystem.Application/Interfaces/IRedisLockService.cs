using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IRedisLockService
    {
        Task<(bool Acquired, string Token)> AcquireLockAsync(string key, TimeSpan expiry);
        Task ReleaseLockAsync(string key, string token);
    }
}