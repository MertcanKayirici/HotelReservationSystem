using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers;

[ApiController]
[Route("api/v1/test")]
public class TestController : ControllerBase
{
    [HttpGet("secure")]
    [Authorize]
    public IActionResult Secure()
    {
        return Ok("Giriş yaptın, burası korumalı alan");
    }
}