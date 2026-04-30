using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _repository;

        public ReservationService(IReservationRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateReservationAsync(CreateReservationDto dto)
        {
            if (dto.CheckInDate < DateTime.Today)
                throw new Exception("Check-in date cannot be in the past");

            var reservation = new Reservation
            {
                RoomId = dto.RoomId,
                UserId = dto.UserId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                CreatedAt = DateTime.Now
            };

            var created = await _repository.TryCreateReservationAsync(reservation);

            if (!created)
                throw new Exception("Room is not available for selected dates");
        }
    }
}