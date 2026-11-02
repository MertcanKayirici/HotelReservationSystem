using HotelReservationSystem.API.Controllers;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests;

public class RoomsControllerTests
{
    private readonly Mock<IRoomService> _roomService = new();

    private RoomsController CreateController()
    {
        return new RoomsController(_roomService.Object);
    }

    private static RoomDto CreateSampleRoom()
    {
        return new RoomDto
        {
            Id = 1,
            Name = "Deluxe Room",
            PricePerNight = 1500,
            Capacity = 2,
            IsAvailable = true
        };
    }

    [Fact]
    public async Task GetAll_WhenRoomsExist_ReturnsOk()
    {
        _roomService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<RoomDto> { CreateSampleRoom() });

        var controller = CreateController();

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rooms = Assert.IsType<List<RoomDto>>(okResult.Value);

        Assert.Single(rooms);
    }

    [Fact]
    public async Task GetAll_WhenNoRoomsExist_ReturnsOkWithEmptyList()
    {
        _roomService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<RoomDto>());

        var controller = CreateController();

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rooms = Assert.IsType<List<RoomDto>>(okResult.Value);

        Assert.Empty(rooms);
    }

    [Fact]
    public async Task GetById_WhenRoomExists_ReturnsOk()
    {
        _roomService
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(CreateSampleRoom());

        var controller = CreateController();

        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<RoomDto>(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomService
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((RoomDto?)null);

        var controller = CreateController();

        var result = await controller.GetById(99);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_ReturnsCreated()
    {
        var dto = new CreateRoomDto
        {
            Name = "Deluxe Room",
            PricePerNight = 1500,
            Capacity = 2,
            IsAvailable = true
        };

        var room = CreateSampleRoom();

        _roomService
            .Setup(x => x.CreateAsync(dto))
            .ReturnsAsync(room);

        var controller = CreateController();

        var result = await controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(nameof(RoomsController.GetById), createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues?["id"]);
        Assert.Equal(room, createdResult.Value);
    }

    [Fact]
    public async Task Update_WhenRoomExists_ReturnsOk()
    {
        var dto = new UpdateRoomDto
        {
            Name = "Royal Suite",
            PricePerNight = 2500,
            Capacity = 3,
            IsAvailable = true
        };

        var room = CreateSampleRoom();

        _roomService
            .Setup(x => x.UpdateAsync(1, dto))
            .ReturnsAsync(room);

        var controller = CreateController();

        var result = await controller.Update(1, dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(room, okResult.Value);
    }

    [Fact]
    public async Task Update_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        var dto = new UpdateRoomDto
        {
            Name = "Royal Suite",
            PricePerNight = 2500,
            Capacity = 3,
            IsAvailable = true
        };

        _roomService
            .Setup(x => x.UpdateAsync(99, dto))
            .ReturnsAsync((RoomDto?)null);

        var controller = CreateController();

        var result = await controller.Update(99, dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenRoomExists_ReturnsNoContent()
    {
        _roomService
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        var controller = CreateController();

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomService
            .Setup(x => x.DeleteAsync(99))
            .ReturnsAsync(false);

        var controller = CreateController();

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}