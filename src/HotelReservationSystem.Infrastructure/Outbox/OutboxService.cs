using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;

/// <summary>
/// Implements outbox message persistence for the Outbox Pattern.
/// Responsible for storing events that will be processed asynchronously.
/// </summary>
public class OutboxService : IOutboxService
{
    /// <summary>
    /// Database context for accessing outbox storage.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes the service with required dependencies.
    /// </summary>
    /// <param name="context">Application database context.</param>
    public OutboxService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a new outbox message to the database context.
    /// Changes are not persisted immediately and should be committed via Unit of Work.
    /// </summary>
    /// <param name="message">Outbox message entity to be stored.</param>
    public async Task AddAsync(OutboxMessage message)
    {
        // Add message to DbContext tracking for later persistence
        await _context.OutboxMessages.AddAsync(message);
    }
}