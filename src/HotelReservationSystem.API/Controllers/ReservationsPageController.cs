using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers;

[Route("Reservations")]
public class ReservationsPageController : Controller
{
    private readonly HttpClient _httpClient;

    public ReservationsPageController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Reservations/Index.cshtml");
    }

    [HttpPost("")]
    public async Task<IActionResult> Create(
        int roomId,
        int userId,
        DateTime checkIn,
        DateTime checkOut)
    {
        var payload = new
        {
            roomId = roomId,
            userId = userId,
            checkIn = checkIn,
            checkOut = checkOut,
            idempotencyKey = Guid.NewGuid().ToString(),
            correlationId = Guid.NewGuid().ToString()
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:5283/api/v1/Reservations",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Rezervasyon oluşturulamadı.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Rezervasyon başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }
}