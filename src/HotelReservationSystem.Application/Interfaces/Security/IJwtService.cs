using HotelReservationSystem.Domain.Entities;

namespace HotelReservationSystem.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}