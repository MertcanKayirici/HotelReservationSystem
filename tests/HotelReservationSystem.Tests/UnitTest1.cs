
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using Moq;
using Xunit;
using HotelReservationSystem.Application.Common;

namespace HotelReservationSystem.Tests;

public class ReservationCoordinatorTests
{
    private readonly Mock<IIdempotencyService> _idempotency = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<IRedisLockService> _lock = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAppLogger> _logger = new();

    private ReservationCoordinator CreateCoordinator() =>
        new(
            _idempotency.Object,
            _outbox.Object,
            _lock.Object,
            _reservationRepository.Object,
            _uow.Object,
            _logger.Object);

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
        // Arrange
        const string key = "same-key-test";

        var coordinator = CreateCoordinator();

        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "lock-token"));

        _idempotency
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(new HotelReservationSystem.Domain.Entities.Idempotency
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

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        // Assert
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
        // Arrange
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
            .ReturnsAsync(new HotelReservationSystem.Domain.Entities.Idempotency
            {
                Key = key,
                Status = "PENDING",
                RequestHash = requestHash,
                Response = "",
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(
            () => coordinator.HandleAsync(dto));

        // Assert
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
            .ReturnsAsync(new HotelReservationSystem.Domain.Entities.Idempotency
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

}
