using HotelReservationSystem.Application.Interfaces;
using Serilog;

public class AppLogger : IAppLogger
{
    public void Info(string message)
        => Log.Information(message);

    public void Info(string message, params object[] args)
        => Log.Information(message, args);

    public void Warning(string message)
        => Log.Warning(message);

    public void Warning(string message, params object[] args)
        => Log.Warning(message, args);

    public void Error(string message)
        => Log.Error(message);

    public void Error(Exception ex, string message, params object[] args)
        => Log.Error(ex, message, args);
}