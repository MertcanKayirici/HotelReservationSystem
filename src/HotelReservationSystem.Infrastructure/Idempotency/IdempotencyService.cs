using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implements idempotency management using the underlying database context.
/// Responsible for tracking request states and ensuring consistent responses for repeated requests.
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    /// <summary>
    /// Database context for accessing idempotency records.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes the service with required dependencies.
    /// </summary>
    /// <param name="context">Application database context.</param>
    public IdempotencyService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Updates the status and response of an existing idempotency record.
    /// Does not persist changes immediately; relies on Unit of Work or external SaveChanges call.
    /// </summary>
    /// <param name="key">Idempotency key.</param>
    /// <param name="status">New status value.</param>
    /// <param name="response">Serialized response payload.</param>
    public async Task UpdateStatusAsync(string key, string status, string response)
    {
        // Retrieve existing idempotency record by key
        var entity = await _context.Idempotencies
            .FirstOrDefaultAsync(x => x.Key == key);

        // Exit if record does not exist
        if (entity == null)
            return;

        // Update status and response fields
        entity.Status = status;
        entity.Response = response;
    }

    /// <summary>
    /// Retrieves an idempotency record by its key.
    /// Used to determine request state and enforce idempotency rules.
    /// </summary>
    /// <param name="key">Idempotency key.</param>
    /// <returns>Idempotency entity if found; otherwise null.</returns>
    public async Task<Idempotency?> GetAsync(string key)
    {
        // Query idempotency record from database
        var result = await _context.Idempotencies
            .FirstOrDefaultAsync(x => x.Key == key);

        // Debug logging for tracing lookup behavior
        Console.WriteLine("DEBUG KEY: " + key);
        Console.WriteLine("FOUND: " + (result != null));

        return result;
    }

    /// <summary>
    /// Creates a new idempotency record in the database context.
    /// Changes are not persisted immediately and should be committed via Unit of Work.
    /// </summary>
    /// <param name="entity">Idempotency entity to add.</param>
    public async Task CreateAsync(Idempotency entity)
    {
        // Add entity to DbContext tracking
        await _context.Idempotencies.AddAsync(entity);

        // SaveChanges intentionally omitted to allow transactional control via Unit of Work
    }
}