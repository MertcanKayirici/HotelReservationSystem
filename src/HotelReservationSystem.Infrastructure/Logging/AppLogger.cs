using HotelReservationSystem.Application.Interfaces;
using Serilog;

/// <summary>
/// Concrete implementation of application-level logging abstraction.
/// Wraps Serilog to provide a consistent logging interface across the application.
/// </summary>
public class AppLogger : IAppLogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">Log message.</param>
    public void Info(string message)
        => Log.Information(message);

    /// <summary>
    /// Logs a formatted informational message with parameters.
    /// </summary>
    /// <param name="message">Message template.</param>
    /// <param name="args">Message arguments.</param>
    public void Info(string message, params object[] args)
        => Log.Information(message, args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">Warning message.</param>
    public void Warning(string message)
        => Log.Warning(message);

    /// <summary>
    /// Logs a formatted warning message with parameters.
    /// </summary>
    /// <param name="message">Message template.</param>
    /// <param name="args">Message arguments.</param>
    public void Warning(string message, params object[] args)
        => Log.Warning(message, args);

    /// <summary>
    /// Logs an error message without exception details.
    /// </summary>
    /// <param name="message">Error message.</param>
    public void Error(string message)
        => Log.Error(message);

    /// <summary>
    /// Logs an error message with exception details and formatted arguments.
    /// </summary>
    /// <param name="ex">Exception instance.</param>
    /// <param name="message">Message template.</param>
    /// <param name="args">Message arguments.</param>
    public void Error(Exception ex, string message, params object[] args)
        => Log.Error(ex, message, args);
}