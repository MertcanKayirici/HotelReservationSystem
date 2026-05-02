using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Repositories;
using HotelReservationSystem.Infrastructure.Security;

namespace HotelReservationSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;

    public AuthService(
        UserRepository userRepository,
        PasswordHasher passwordHasher,
        JwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<string> RegisterAsync(string email, string password)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
            throw new Exception("User already exists");

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(password)
        };

        await _userRepository.AddAsync(user);

        return _jwtService.GenerateToken(user);
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new Exception("User not found");

        var valid = _passwordHasher.Verify(password, user.PasswordHash);

        if (!valid)
            throw new Exception("Invalid credentials");

        return _jwtService.GenerateToken(user);
    }
}