public interface IAuthService
{
    Task<string> RegisterAsync(string email, string password);

    Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password);

    Task<string> RefreshTokenAsync(string refreshToken);

    Task LogoutAsync(string refreshToken);
}