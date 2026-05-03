using HotelReservationSystem.API.Models.Requests;
using HotelReservationSystem.API.Models.Responses;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _authService.RegisterAsync(request.Email, request.Password);

        return Ok(new
        {
            success = true,
            data = new AuthResponse
            {
                Token = token
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _authService.LoginAsync(request.Email, request.Password);

        return Ok(new
        {
            success = true,
            data = new AuthResponse
            {
                Token = token
            }
        });
    }
}