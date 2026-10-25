
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Exceptions;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Application.Services;
using System.Security.Cryptography;

namespace HotelReservationSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<(string accessToken, string refreshToken)> RegisterAsync(
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
            throw new UserAlreadyExistsException();

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            Role = "User"
        };

        await _userRepository.AddAsync(user);

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = CreateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _refreshTokenRepository.SaveChangesAsync();

        return (accessToken, refreshToken);
    }
    public async Task<(string accessToken, string refreshToken)> LoginAsync(
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new InvalidCredentialsException();

        var valid = _passwordHasher.Verify(password, user.PasswordHash);

        if (!valid)
            throw new InvalidCredentialsException();

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = CreateRefreshToken();

        var tokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.AddAsync(tokenEntity);
        await _refreshTokenRepository.SaveChangesAsync();

        return (accessToken, refreshToken);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(
        string refreshToken)
    {
        var token = await _refreshTokenRepository.GetAsync(refreshToken);

        if (token == null || token.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidRefreshTokenException();

        var user = await _userRepository.GetByIdAsync(token.UserId);

        if (user == null)
            throw new UserNotFoundException();

        var newRefreshToken = CreateRefreshToken();

        var newTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var rotated = await _refreshTokenRepository.RotateAsync(
            refreshToken,
            newTokenEntity);

        if (!rotated)
            throw new InvalidRefreshTokenException();

        var newAccessToken = _jwtService.GenerateToken(user);

        return (newAccessToken, newRefreshToken);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        await _refreshTokenRepository.RevokeAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
