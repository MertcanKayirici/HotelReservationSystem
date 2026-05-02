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

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomService.GetByIdAsync(id);

        if (room is null)
            return NotFound();

        var model = new UpdateRoomDto
        {
            Name = room.Name,
            Price = room.Price,
            Capacity = room.Capacity,
            IsActive = room.IsActive
        };

        return View("~/Views/Rooms/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, UpdateRoomDto dto)
    {
        await _roomService.UpdateAsync(id, dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
