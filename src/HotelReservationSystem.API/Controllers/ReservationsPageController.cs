using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers;

[Route("Reservations")]
public class ReservationsPageController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Reservations/Index.cshtml");
    }
}