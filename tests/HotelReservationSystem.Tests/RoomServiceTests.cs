
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Application.DTOs;
using Moq;


namespace HotelReservationSystem.Tests;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();

    private RoomService CreateService()
    {
        return new RoomService(_roomRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenRoomsExist_ReturnsAllRooms()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new Room
            {
                Id = 1,
                Name = "Deluxe Room",
                PricePerNight = 1500,
                Capacity = 2,
                IsAvailable = true
            },
            new Room
            {
                Id = 2,
                Name = "Family Room",
                PricePerNight = 2500,
                Capacity = 4,
                IsAvailable = false
            }
        };

        _roomRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(rooms);

        var service = CreateService();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Deluxe Room", result[0].Name);
        Assert.Equal("Family Room", result[1].Name);

        _roomRepository.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomExists_ReturnsRoom()
    {
        // Arrange
        var room = new Room
        {
            Id = 1,
            Name = "Deluxe Room",
            PricePerNight = 1500,
            Capacity = 2,
            IsAvailable = true
        };

        _roomRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(room);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Deluxe Room", result.Name);
        Assert.Equal(1500, result.PricePerNight);
        Assert.Equal(2, result.Capacity);
        Assert.True(result.IsAvailable);

        _roomRepository.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        // Arrange
        _roomRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Room?)null);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);

        _roomRepository.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValidRoom_ReturnsCreatedRoom()
    {
        // Arrange
        var dto = new CreateRoomDto
        {
            Name = "  Luxury Room  ",
            PricePerNight = 2000,
            Capacity = 3,
            IsAvailable = true
        };

        _roomRepository
            .Setup(x => x.AddAsync(It.IsAny<Room>()))
            .Returns(Task.CompletedTask);

        _roomRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Luxury Room", result.Name);
        Assert.Equal(2000, result.PricePerNight);
        Assert.Equal(3, result.Capacity);
        Assert.True(result.IsAvailable);

        _roomRepository.Verify(
            x => x.AddAsync(It.Is<Room>(
                room => room.Name == "Luxury Room")),
            Times.Once);

        _roomRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task UpdateAsync_WhenRoomExists_ReturnsUpdatedRoom()
    {
        // Arrange
        var room = new Room
        {
            Id = 1,
            Name = "Standard Room",
            PricePerNight = 1000,
            Capacity = 2,
            IsAvailable = true
        };

        var dto = new UpdateRoomDto
        {
            Name = "  Deluxe Room  ",
            PricePerNight = 2000,
            Capacity = 3,
            IsAvailable = false
        };

        _roomRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(room);

        _roomRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Deluxe Room", result.Name);
        Assert.Equal(2000, result.PricePerNight);
        Assert.Equal(3, result.Capacity);
        Assert.False(result.IsAvailable);

        _roomRepository.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);

        _roomRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task UpdateAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateRoomDto
        {
            Name = "Deluxe Room",
            PricePerNight = 2000,
            Capacity = 3,
            IsAvailable = true
        };

        _roomRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Room?)null);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(999, dto);

        // Assert
        Assert.Null(result);

        _roomRepository.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);

        _roomRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task DeleteAsync_WhenRoomExists_ReturnsTrue()
    {
        // Arrange
        _roomRepository
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        _roomRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1);

        // Assert
        Assert.True(result);

        _roomRepository.Verify(
            x => x.DeleteAsync(1),
            Times.Once);

        _roomRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_WhenRoomDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _roomRepository
            .Setup(x => x.DeleteAsync(999))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        Assert.False(result);

        _roomRepository.Verify(
            x => x.DeleteAsync(999),
            Times.Once);

        _roomRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
}

