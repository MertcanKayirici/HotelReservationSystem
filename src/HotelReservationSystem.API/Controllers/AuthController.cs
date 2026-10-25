
using HotelReservationSystem.API.Models.Requests;
using HotelReservationSystem.API.Models.Responses;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelReservationSystem.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
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
        var result = await _authService.RegisterAsync(
            request.Email,
            request.Password);

        return Ok(new
        {
            success = true,
            data = new AuthResponse
            {
                AccessToken = result.accessToken,
                RefreshToken = result.refreshToken
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(
            request.Email,
            request.Password);

        return Ok(new
        {
            success = true,
            data = new AuthResponse
            {
                AccessToken = result.accessToken,
                RefreshToken = result.refreshToken
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(
            request.RefreshToken);

        return Ok(new
        {
            success = true,
            data = new AuthResponse
            {
                AccessToken = result.accessToken,
                RefreshToken = result.refreshToken
            }
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);

        return Ok(new { success = true });
    }
}
