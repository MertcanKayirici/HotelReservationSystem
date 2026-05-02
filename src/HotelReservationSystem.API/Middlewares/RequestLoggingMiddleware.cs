using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HotelReservationSystem.API.Middlewares
{
    /// <summary>
    /// Middleware responsible for logging incoming HTTP requests and outgoing responses,
    /// including execution time for performance monitoring.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        /// <summary>
        /// Reference to the next middleware in the pipeline.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes the middleware with the next delegate.
        /// </summary>
        /// <param name="next">Next middleware delegate.</param>
        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Intercepts the HTTP request, logs request details, measures execution time,
        /// and logs the response after processing is complete.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Start timing the request for performance tracking
            var stopwatch = Stopwatch.StartNew();

            // Extract request metadata
            var method = context.Request.Method;
            var path = context.Request.Path;

            // Log incoming request details
            Log.Information("[REQUEST] {Method} {Path}", method, path);

            // Continue processing the request pipeline
            await _next(context);

            // Stop timing after response is generated
            stopwatch.Stop();

            // Capture response status code
            var statusCode = context.Response.StatusCode;

            // Log response details along with execution duration in milliseconds
            Log.Information("[RESPONSE] {StatusCode} ({Elapsed} ms)", statusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}