using HotelReservationSystem.Application.DTOs;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IReservationCoordinator
    {
        Task HandleAsync(CreateReservationDto dto);
    }
}