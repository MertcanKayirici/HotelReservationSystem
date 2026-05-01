using HotelReservationSystem.Domain.Entities;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IOutboxService
    {
        Task AddAsync(OutboxMessage message);
    }
}