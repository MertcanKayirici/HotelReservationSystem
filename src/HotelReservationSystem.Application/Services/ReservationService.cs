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
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE");

            if (string.IsNullOrEmpty(dto.IdempotencyKey))
                throw new Exception("Idempotency key required");

            // sadece request kabul edildi
        }
    }
}