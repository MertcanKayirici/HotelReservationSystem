using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationCoordinator _coordinator;

        public ReservationService(IReservationCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public async Task CreateReservationAsync(CreateReservationDto dto)
        {
            await _coordinator.HandleAsync(dto);
        }
    }
}