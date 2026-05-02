using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers;

[Route("Rooms")]
public class RoomsPageController : Controller
{
    private readonly IRoomService _roomService;

    public RoomsPageController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rooms = await _roomService.GetAllAsync();
        return View("~/Views/Rooms/Index.cshtml", rooms);
    }
}