
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Domain.Entities;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests;

public class ReservationCoordinatorActiveRoomTests
{
    private readonly Mock<IIdempotencyService> _idempotency = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<IRedisLockService> _lock = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAppLogger> _logger = new();

    private ReservationCoordinator CreateCoordinator()
    {
        _lock
            .Setup(x => x.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((true, "test-token"));

        _idempotency
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((Idempotency?)null);

        _uow
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());

        return new ReservationCoordinator(
            _idempotency.Object,
            _outbox.Object,
            _lock.Object,
            _reservationRepository.Object,
            _roomRepository.Object,
            _uow.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIsActive_CreatesOutboxMessage()
    {
        _roomRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Room
            {
                Id = 1,
                Name = "Active Room",
                Capacity = 2,
                PricePerNight = 1500,
                IsAvailable = true,
                IsActive = true
            });

        var coordinator = CreateCoordinator();

        var request = new CreateReservationDto
        {
            RoomId = 1,
            UserId = 10,
            CheckIn = new DateTime(2026, 12, 10),
            CheckOut = new DateTime(2026, 12, 15),
            IdempotencyKey = "test-reservation-001"
        };

        await coordinator.HandleAsync(request);

        _idempotency.Verify(
            x => x.CreateAsync(It.Is<Idempotency>(
                item => item.Status == "PENDING")),
            Times.Once);

        _outbox.Verify(
            x => x.AddAsync(It.Is<OutboxMessage>(
                item => item.Type == "ReservationCreated")),
            Times.Once);

        _uow.Verify(
            x => x.ExecuteAsync(It.IsAny<Func<Task>>()),
            Times.Once);

        _lock.Verify(
            x => x.ReleaseLockAsync(
                "lock:room:1",
                "test-token"),
            Times.Once);
    }
}
