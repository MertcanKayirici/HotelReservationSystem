using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _context;

    public IdempotencyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Idempotency?> GetAsync(string key)
    {
        var result = await _context.Idempotencies
            .FirstOrDefaultAsync(x => x.Key == key);

        Console.WriteLine("DEBUG KEY: " + key);
        Console.WriteLine("FOUND: " + (result != null));

        return result;
    }

    public async Task CreateAsync(Idempotency entity)
    {
        await _context.Idempotencies.AddAsync(entity);
        await _context.SaveChangesAsync();
    }
}