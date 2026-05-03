using HotelReservationSystem.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetAsync(string token);
    Task SaveChangesAsync();
    Task RevokeAsync(string token);
}