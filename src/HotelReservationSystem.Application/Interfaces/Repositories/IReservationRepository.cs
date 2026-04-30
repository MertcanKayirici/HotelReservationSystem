using HotelReservationSystem.Domain.Entities;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {
        Task AddAsync(Reservation reservation);
    }
}