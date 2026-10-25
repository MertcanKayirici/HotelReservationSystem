
using HotelReservationSystem.Application.Exceptions;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Domain.Entities;
using Moq;

namespace HotelReservationSystem.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();

    private AuthService CreateService() =>
        new(
            _users.Object,
            _hasher.Object,
            _jwt.Object,
            _refreshTokens.Object);

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ThrowsUserAlreadyExists()
    {
        _users
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(new User { Id = 1, Email = "test@example.com" });

        var service = CreateService();

        await Assert.ThrowsAsync<UserAlreadyExistsException>(
            () => service.RegisterAsync("Test@example.com", "Password123!"));

        _users.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenNewUser_CreatesHashedPasswordAndReturnsTokens()
    {
        _users
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync((User?)null);

        _hasher
            .Setup(x => x.Hash("Password123!"))
            .Returns("hashed-password");

        _users
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => user.Id = 7)
            .Returns(Task.CompletedTask);

        _jwt
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("access-token");

        _refreshTokens
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        _refreshTokens
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.RegisterAsync(
            " Test@Example.com ",
            "Password123!");

        Assert.Equal("access-token", result.accessToken);
        Assert.False(string.IsNullOrWhiteSpace(result.refreshToken));

        _users.Verify(
            x => x.AddAsync(It.Is<User>(u =>
                u.Email == "test@example.com" &&
                u.PasswordHash == "hashed-password")),
            Times.Once);

        _refreshTokens.Verify(
            x => x.AddAsync(It.Is<RefreshToken>(t =>
                t.UserId == 7 &&
                t.Token == result.refreshToken)),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ThrowsInvalidCredentials()
    {
        _users
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = "stored-hash"
            });

        _hasher
            .Setup(x => x.Verify("WrongPassword!", "stored-hash"))
            .Returns(false);

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync("test@example.com", "WrongPassword!"));

        _refreshTokens.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRotationFails_ThrowsInvalidRefreshToken()
    {
        const string oldToken = "old-refresh-token";

        _refreshTokens
            .Setup(x => x.GetAsync(oldToken))
            .ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 7,
                Token = oldToken,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            });

        _users
            .Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(new User
            {
                Id = 7,
                Email = "test@example.com"
            });

        _refreshTokens
            .Setup(x => x.RotateAsync(
                oldToken,
                It.IsAny<RefreshToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => service.RefreshTokenAsync(oldToken));
    }
}
