using HotelReservationSystem.Application.DTOs;
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
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View("~/Views/Rooms/Create.cshtml");
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        await _roomService.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }
}
