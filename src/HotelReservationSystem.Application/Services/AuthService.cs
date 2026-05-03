using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using static HotelReservationSystem.Application.Exceptions.AuthExceptions;

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

    public async Task<string> RegisterAsync(string email, string password)
    {
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

        return _jwtService.GenerateToken(user);
    }

    // 🔥 BURASI DEĞİŞTİ
    public async Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new Exception("USER_NOT_FOUND");

        var valid = _passwordHasher.Verify(password, user.PasswordHash);

        if (!valid)
            throw new InvalidCredentialsException();

        var accessToken = _jwtService.GenerateToken(user);

        var refreshToken = Guid.NewGuid().ToString();

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

    // 🔥 YENİ METHOD
    public async Task<string> RefreshTokenAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetAsync(refreshToken);

        if (token == null || token.ExpiresAt < DateTime.UtcNow)
            throw new Exception("INVALID_REFRESH_TOKEN");

        var user = await _userRepository.GetByIdAsync(token.UserId);

        if (user == null)
            throw new Exception("USER_NOT_FOUND");

        return _jwtService.GenerateToken(user);
    }
}