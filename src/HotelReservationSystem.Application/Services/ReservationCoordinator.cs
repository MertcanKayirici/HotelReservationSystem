using HotelReservationSystem.Application.Common;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using System.Text.Json;

namespace HotelReservationSystem.Application.Services
{
    /// <summary>
    /// Orchestrates the full reservation workflow including validation,
    /// idempotency handling, distributed locking, persistence, and event publishing.
    /// </summary>
    public class ReservationCoordinator : IReservationCoordinator
    {
        /// <summary>
        /// Service for managing idempotent request lifecycle.
        /// </summary>
        private readonly IIdempotencyService _idempotency;

        /// <summary>
        /// Service responsible for persisting outbox messages.
        /// </summary>
        private readonly IOutboxService _outbox;

        /// <summary>
        /// Distributed locking service to prevent concurrent reservation conflicts.
        /// </summary>
        private readonly IRedisLockService _lock;

        /// <summary>
        /// Repository for reservation data access.
        /// </summary>
        private readonly IReservationRepository _reservationRepository;

        /// <summary>
        /// Unit of Work abstraction for transactional consistency.
        /// </summary>
        private readonly IUnitOfWork _uow;

        /// <summary>
        /// Application-level logger abstraction.
        /// </summary>
        private readonly IAppLogger _logger;

        /// <summary>
        /// Initializes coordinator with required dependencies.
        /// </summary>
        public ReservationCoordinator(
            IIdempotencyService idempotency,
            IOutboxService outbox,
            IRedisLockService lockService,
            IReservationRepository reservationRepository,
            IUnitOfWork uow,
            IAppLogger logger)
        {
            _idempotency = idempotency;
            _outbox = outbox;
            _lock = lockService;
            _reservationRepository = reservationRepository;
            _uow = uow;
            _logger = logger;
        }

        /// <summary>
        /// Handles reservation creation workflow end-to-end.
        /// Includes validation, locking, idempotency checks, transaction execution,
        /// and event publishing via outbox pattern.
        /// </summary>
        /// <param name="dto">Reservation request data.</param>
        public async Task HandleAsync(CreateReservationDto dto)
        {
            // Validate date range
            if (dto.CheckOut <= dto.CheckIn)
                throw new Exception("INVALID_DATE_RANGE");

            // Validate idempotency key presence
            if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
                throw new Exception("IDEMPOTENCY_KEY_REQUIRED");

            // Build distributed lock key based on room identifier
            var lockKey = $"lock:room:{dto.RoomId}";

            // Attempt to acquire lock to prevent concurrent bookings for the same room
            var (acquired, token) = await _lock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(20));

            if (!acquired)
                throw new Exception("ROOM_LOCKED");

            _logger.Info("Before UOW");

            try
            {
                // Generate deterministic hash for request payload (used for idempotency validation)
                var requestHash = RequestHashHelper.Compute(new
                {
                    dto.RoomId,
                    dto.UserId,
                    dto.CheckIn,
                    dto.CheckOut
                });

                // Check existing idempotency record
                var existing = await _idempotency.GetAsync(dto.IdempotencyKey);

                if (existing != null)
                {
                    // Same key used with different payload
                    if (existing.RequestHash != requestHash)
                        throw new Exception("IDEMPOTENCY_KEY_CONFLICT");

                    // Request is still being processed
                    if (existing.Status == "PENDING")
                        throw new Exception("IN_PROGRESS");

                    // Request already completed previously
                    if (existing.Status == "COMPLETED")
                        throw new Exception("ALREADY_COMPLETED");
                }

                _logger.Info("Inside UOW for {RoomId}", dto.RoomId);

                // Execute transactional operations
                await _uow.ExecuteAsync(async () =>
                {
                    _logger.Info("Inside UOW for {RoomId}", dto.RoomId);

                    // Check for reservation conflicts within the requested date range
                    var hasConflict = await _reservationRepository.HasConflictAsync(
                        dto.RoomId,
                        dto.CheckIn,
                        dto.CheckOut
                    );

                    if (hasConflict)
                        throw new Exception("ROOM_CONFLICT");

                    // Create idempotency record with initial PENDING state
                    await _idempotency.CreateAsync(new Idempotency
                    {
                        Key = dto.IdempotencyKey,
                        Status = "PENDING",
                        RequestHash = requestHash,
                        Response = "",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Add reservation event to outbox for asynchronous processing
                    await _outbox.AddAsync(new OutboxMessage
                    {
                        Type = "ReservationCreated",
                        Payload = JsonSerializer.Serialize(dto),
                        CreatedAt = DateTime.UtcNow,
                        Processed = false,
                        Dead = false,
                        RetryCount = 0
                    });

                    _logger.Info($"Outbox created for RoomId: {dto.RoomId}");
                });

                _logger.Info("After UOW for {RoomId}", dto.RoomId);

                // Update idempotency record with COMPLETED status and response payload
                await _idempotency.UpdateStatusAsync(
                    dto.IdempotencyKey,
                    "COMPLETED",
                    JsonSerializer.Serialize(new
                    {
                        message = "Reservation created successfully",
                        roomId = dto.RoomId,
                        userId = dto.UserId,
                        checkIn = dto.CheckIn,
                        checkOut = dto.CheckOut
                    })
                );
            }
            finally
            {
                // Ensure distributed lock is always released
                await _lock.ReleaseLockAsync(lockKey, token);
            }
        }
    }
}