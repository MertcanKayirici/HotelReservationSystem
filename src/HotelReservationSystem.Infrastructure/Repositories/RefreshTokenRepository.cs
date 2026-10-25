using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
    }

    public async Task<RefreshToken?> GetAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.Token == token &&
                !x.IsRevoked &&
                x.ExpiresAt > DateTime.UtcNow);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAsync(string token)
    {
        var entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked);

        if (entity != null)
        {
            entity.IsRevoked = true;
        }
    }

    public async Task<bool> RotateAsync(
        string currentToken,
        RefreshToken replacement)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;

        var affectedRows = await _context.RefreshTokens
            .Where(x =>
                x.Token == currentToken &&
                !x.IsRevoked &&
                x.ExpiresAt > now)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(x => x.IsRevoked, true));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await _context.RefreshTokens.AddAsync(replacement);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }
}
