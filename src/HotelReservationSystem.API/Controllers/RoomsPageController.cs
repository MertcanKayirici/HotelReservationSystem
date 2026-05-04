using System.Net.Http.Json;
using HotelReservationSystem.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers;

[Route("Rooms")]
public class RoomsPageController : Controller
{
    private readonly HttpClient _httpClient;

    public RoomsPageController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rooms = await _httpClient.GetFromJsonAsync<List<RoomDto>>(
            "https://localhost:7140/api/v1/Rooms");

        return View("~/Views/Rooms/Index.cshtml", rooms ?? new List<RoomDto>());
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View("~/Views/Rooms/Create.cshtml");
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "https://localhost:7140/api/v1/Rooms",
            dto);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Oda oluşturulamadı.");
            return View("~/Views/Rooms/Create.cshtml", dto);
        }

        return RedirectToAction(nameof(Index));
    }
    [HttpGet("Edit/{id:int}")]
public async Task<IActionResult> Edit(int id)
{
    var room = await _httpClient.GetFromJsonAsync<RoomDto>(
        $"https://localhost:7140/api/v1/Rooms/{id}");

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
    var response = await _httpClient.PutAsJsonAsync(
        $"https://localhost:7140/api/v1/Rooms/{id}",
        dto);

    if (!response.IsSuccessStatusCode)
    {
        ModelState.AddModelError("", "Oda güncellenemedi.");
        return View("~/Views/Rooms/Edit.cshtml", dto);
    }

    return RedirectToAction(nameof(Index));
}

[HttpGet("Delete/{id:int}")]
public async Task<IActionResult> Delete(int id)
{
    var response = await _httpClient.DeleteAsync(
        $"https://localhost:7140/api/v1/Rooms/{id}");

    if (!response.IsSuccessStatusCode)
    {
        TempData["Error"] = "Oda silinemedi.";
    }
    else
    {
        TempData["Success"] = "Oda başarıyla silindi.";
    }

    return RedirectToAction(nameof(Index));
}
}