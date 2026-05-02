using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    /// <summary>
    /// Abstraction for application-level logging.
    /// Provides a consistent interface for structured logging across the system.
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">Log message.</param>
        void Info(string message);

        /// <summary>
        /// Logs a formatted informational message with parameters.
        /// </summary>
        /// <param name="message">Message template.</param>
        /// <param name="args">Message arguments.</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">Warning message.</param>
        void Warning(string message);

        /// <summary>
        /// Logs a formatted warning message with parameters.
        /// </summary>
        /// <param name="message">Message template.</param>
        /// <param name="args">Message arguments.</param>
        void Warning(string message, params object[] args);

        /// <summary>
        /// Logs an error message without exception details.
        /// </summary>
        /// <param name="message">Error message.</param>
        void Error(string message);

        /// <summary>
        /// Logs an error message with exception details and formatted arguments.
        /// </summary>
        /// <param name="ex">Exception instance.</param>
        /// <param name="message">Message template.</param>
        /// <param name="args">Message arguments.</param>
        void Error(Exception ex, string message, params object[] args);
    }
}