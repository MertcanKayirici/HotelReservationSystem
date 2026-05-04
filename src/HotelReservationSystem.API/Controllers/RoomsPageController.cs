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
}