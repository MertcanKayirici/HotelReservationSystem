using HotelReservationSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class AppLogger : IAppLogger
{
    private readonly ILogger<AppLogger> _logger;

    public AppLogger(ILogger<AppLogger> logger)
    {
        _logger = logger;
    }

    public void Info(string message)
    {
        _logger.LogInformation(message);
    }

    public void Warning(string message)
    {
        _logger.LogWarning(message);
    }

    public void Error(string message)
    {
        _logger.LogError(message);
    }
}