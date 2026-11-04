
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Application.Enums;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();

    private RoomService CreateService()
    {
        return new RoomService(_roomRepository.Object);
    }

    private static Room CreateTestRoom(bool isActive = true)
    {
        return new Room
        {
            Id = 1,
            Name = "Test Room",
            PricePerNight = 1500,
            Capacity = 2,
            IsAvailable = true,
            IsActive = isActive
        };
    }

    [Fact]
    public async Task ActivateAsync_WhenRoomExists_ActivatesRoom()
    {
        var room = CreateTestRoom(false);

        _roomRepository
            .Setup(x => x.SetActiveWithLockAsync(
                1,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                room.IsActive = true;
                return room;
            });

        var service = CreateService();

        var result = await service.ActivateAsync(1);

        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.Equal(1, result.Id);

        _roomRepository.Verify(
            x => x.SetActiveWithLockAsync(
                1,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenRoomExists_DeactivatesRoom()
    {
        var room = CreateTestRoom(true);

        _roomRepository
            .Setup(x => x.SetActiveWithLockAsync(
                1,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                room.IsActive = false;
                return room;
            });

        var service = CreateService();

        var result = await service.DeactivateAsync(1);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal(1, result.Id);

        _roomRepository.Verify(
            x => x.SetActiveWithLockAsync(
                1,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        _roomRepository
            .Setup(x => x.SetActiveWithLockAsync(
                999,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var service = CreateService();

        var result = await service.ActivateAsync(999);

        Assert.Null(result);

        _roomRepository.Verify(
            x => x.SetActiveWithLockAsync(
                999,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        _roomRepository
            .Setup(x => x.SetActiveWithLockAsync(
                999,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var service = CreateService();

        var result = await service.DeactivateAsync(999);

        Assert.Null(result);

        _roomRepository.Verify(
            x => x.SetActiveWithLockAsync(
                999,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRooms()
    {
        var rooms = new List<Room>
        {
            CreateTestRoom(true),
            new Room
            {
                Id = 2,
                Name = "Inactive Room",
                PricePerNight = 2000,
                Capacity = 3,
                IsAvailable = true,
                IsActive = false
            }
        };

        _roomRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(rooms);

        var service = CreateService();

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsActive);
        Assert.False(result[1].IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomExists_ReturnsRoom()
    {
        var room = CreateTestRoom();

        _roomRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(room);

        var service = CreateService();

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        _roomRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Room?)null);

        var service = CreateService();

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoomIsInactive_KeepsRoomInactive()
    {
        var room = CreateTestRoom(false);

        var dto = new UpdateRoomDto
        {
            Name = "Updated Room",
            PricePerNight = 2000,
            Capacity = 3,
            IsAvailable = true
        };

        _roomRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(room);

        var service = CreateService();

        var result = await service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("Updated Room", result.Name);
        Assert.Equal(2000, result.PricePerNight);
        Assert.Equal(3, result.Capacity);

        _roomRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenRoomIsDeleted_ReturnsDeleted()
    {
        _roomRepository
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoomDeletionResult.Deleted);

        var service = CreateService();

        var result = await service.DeleteAsync(1);

        Assert.Equal(RoomDeletionResult.Deleted, result);
    }


    [Fact]
    public async Task DeleteAsync_WhenRoomHasDependencies_ReturnsHasDependencies()
    {
        _roomRepository
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoomDeletionResult.HasDependencies);

        var service = CreateService();

        var result = await service.DeleteAsync(1);

        Assert.Equal(RoomDeletionResult.HasDependencies, result);
    }

    [Fact]
    public async Task DeleteAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomRepository
            .Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoomDeletionResult.NotFound);

        var service = CreateService();

        var result = await service.DeleteAsync(999);

        Assert.Equal(RoomDeletionResult.NotFound, result);
    }
}
