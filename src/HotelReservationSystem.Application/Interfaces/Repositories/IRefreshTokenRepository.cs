
using HotelReservationSystem.Domain.Entities;

namespace HotelReservationSystem.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetAsync(string token);
    Task SaveChangesAsync();
    Task RevokeAsync(string token);

    Task<bool> RotateAsync(string currentToken, RefreshToken replacement);
}
