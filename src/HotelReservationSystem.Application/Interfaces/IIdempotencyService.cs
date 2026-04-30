using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    using HotelReservationSystem.Domain.Entities;

    public interface IIdempotencyService
    {
        Task UpdateStatusAsync(string key, string status, string response);
        Task<Idempotency> GetAsync(string key);
        Task CreateAsync(Idempotency entity);
    }
}
