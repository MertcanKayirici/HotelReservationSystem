
using HotelReservationSystem.API.Controllers;
using HotelReservationSystem.Application.Enums;
using HotelReservationSystem.Application.Interfaces;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.Tests;

public class RoomsControllerTests
{
    private readonly Mock<IRoomService> _roomService = new();

    private RoomsController CreateController()
    {
        return new RoomsController(_roomService.Object);
    }

    [Fact]
    public async Task Delete_WhenRoomIsDeleted_ReturnsNoContent()
    {
        _roomService
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(RoomDeletionResult.Deleted);

        var controller = CreateController();

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenRoomHasDependencies_ReturnsConflict()
    {
        _roomService
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(RoomDeletionResult.HasDependencies);

        var controller = CreateController();

        var result = await controller.Delete(1);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomService
            .Setup(x => x.DeleteAsync(999))
            .ReturnsAsync(RoomDeletionResult.NotFound);

        var controller = CreateController();

        var result = await controller.Delete(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }
}
