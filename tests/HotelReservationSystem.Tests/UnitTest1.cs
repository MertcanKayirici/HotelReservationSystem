
using HotelReservationSystem.Application.Common;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Domain.Entities;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests;

public class ReservationCoordinatorTests
{
    private readonly Mock<IIdempotencyService> _idempotency = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<IRedisLockService> _lock = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAppLogger> _logger = new();

    private ReservationCoordinator CreateCoordinator() =>
        new(
            _idempotency.Object,
            _outbox.Object,
            _lock.Object,
            _reservationRepository.Object,
            _roomRepository.Object,
            _uow.Object,
            _logger.Object);

    private void SetupActiveRoom()
    {
        _roomRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Room
            {
                Id = 1,
                Name = "Test Room",
                IsActive = true
            });
    }

    [Fact]
    public async Task HandleAsync_WhenCheckOutIsBeforeCheckIn_ThrowsInvalidDateRange()
    {
        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 12),
            CheckOut = new DateTime(2026, 11, 10),
            IdempotencyKey = "test-invalid-date"
        };

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("INVALID_DATE_RANGE", exception.Message);

        _lock.Verify(
            x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIdempotencyKeyIsEmpty_ThrowsRequiredKeyError()
    {
        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = ""
        };

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", exception.Message);

        _lock.Verify(
            x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSameKeyHasDifferentRequest_ThrowsConflict()
    {
        const string key = "same-key-test";

        var coordinator = CreateCoordinator();

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _idempotency
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(new Idempotency
            {
                Key = key,
                Status = "PENDING",
                RequestHash = "different-request-hash",
                Response = "",
                CreatedAt = DateTime.UtcNow
            });

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = key
        };

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", exception.Message);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Never);

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:1", "lock-token"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSameKeyAndSameRequestIsPending_ThrowsInProgress()
    {
        const string key = "same-key-pending-test";

        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = key
        };

        var requestHash = RequestHashHelper.Compute(new
        {
            dto.RoomId,
            dto.UserId,
            dto.CheckIn,
            dto.CheckOut
        });

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _idempotency
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(new Idempotency
            {
                Key = key,
                Status = "PENDING",
                RequestHash = requestHash,
                Response = "",
                CreatedAt = DateTime.UtcNow
            });

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("IN_PROGRESS", exception.Message);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Never);

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:1", "lock-token"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSameKeyAndSameRequestIsCompleted_ThrowsAlreadyCompleted()
    {
        const string key = "same-key-completed-test";

        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = key
        };

        var requestHash = RequestHashHelper.Compute(new
        {
            dto.RoomId,
            dto.UserId,
            dto.CheckIn,
            dto.CheckOut
        });

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _idempotency
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(new Idempotency
            {
                Key = key,
                Status = "COMPLETED",
                RequestHash = requestHash,
                Response = "{\"message\":\"Reservation created successfully\"}",
                CreatedAt = DateTime.UtcNow
            });

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("ALREADY_COMPLETED", exception.Message);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Never);

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:1", "lock-token"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUnitOfWorkFails_ReleasesRoomLock()
    {
        var coordinator = CreateCoordinator();

        SetupActiveRoom();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = "uow-failure-lock-release-test"
        };

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _idempotency
            .Setup(x => x.GetAsync(dto.IdempotencyKey))
            .ReturnsAsync((Idempotency?)null);

        _uow
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<Task>>()))
            .ThrowsAsync(new InvalidOperationException("Simulated UOW failure"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("Simulated UOW failure", exception.Message);

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:1", "lock-token"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomLockCannotBeAcquired_ThrowsRoomLocked()
    {
        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = "room-locked-test"
        };

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((false, string.Empty));

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("ROOM_LOCKED", exception.Message);

        _idempotency.Verify(
            x => x.GetAsync(It.IsAny<string>()),
            Times.Never);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Never);

        _lock.Verify(
            x => x.ReleaseLockAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIdempotencyKeyRaceOccurs_UsesExistingRecord()
    {
        const string key = "idempotency-race-test";

        var coordinator = CreateCoordinator();

        SetupActiveRoom();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = key
        };

        var requestHash = RequestHashHelper.Compute(new
        {
            dto.RoomId,
            dto.UserId,
            dto.CheckIn,
            dto.CheckOut
        });

        var existingRecord = new Idempotency
        {
            Key = key,
            Status = "PENDING",
            RequestHash = requestHash,
            Response = "",
            CreatedAt = DateTime.UtcNow
        };

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _idempotency
            .SetupSequence(x => x.GetAsync(key))
            .ReturnsAsync((Idempotency?)null)
            .ReturnsAsync(existingRecord);

        _uow
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<Task>>()))
            .ThrowsAsync(new HotelReservationSystem.Application.Exceptions
                .IdempotencyKeyAlreadyExistsException(
                    new Exception("Simulated unique-key conflict")));

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("IN_PROGRESS", exception.Message);

        _idempotency.Verify(
            x => x.GetAsync(key),
            Times.Exactly(2));

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:1", "lock-token"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIsInactive_ThrowsRoomInactive()
    {
        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = "inactive-room-test"
        };

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _roomRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Room
            {
                Id = 1,
                Name = "Inactive Room",
                IsActive = false
            });

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("ROOM_INACTIVE", exception.Message);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Never);

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:1", "lock-token"),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomNotFound()
    {
        var coordinator = CreateCoordinator();

        var dto = new CreateReservationDto
        {
            RoomId = 999,
            UserId = 1,
            CheckIn = new DateTime(2026, 11, 10),
            CheckOut = new DateTime(2026, 11, 13),
            IdempotencyKey = "missing-room-test"
        };

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _roomRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Room?)null);

        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        Assert.Equal("ROOM_NOT_FOUND", exception.Message);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Never);

        _lock.Verify(
            x => x.ReleaseLockAsync("lock:room:999", "lock-token"),
            Times.Once);
    }
}
