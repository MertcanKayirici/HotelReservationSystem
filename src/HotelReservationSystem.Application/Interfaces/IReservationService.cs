using HotelReservationSystem.Application.DTOs;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IReservationService
    {
        Task CreateReservationAsync(CreateReservationDto dto);
    }
}