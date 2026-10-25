using Microsoft.AspNetCore.Http;
using System.Net;
using HotelReservationSystem.Application.Services;
using System.Threading.Tasks;
using static HotelReservationSystem.Application.Services.ReservationService;

namespace HotelReservationSystem.API.Middlewares
{
    /// <summary>
    /// Global exception handling middleware responsible for catching unhandled exceptions
    /// and returning standardized API responses.
    /// </summary>
    public class ExceptionMiddleware
    {
        /// <summary>
        /// Reference to the next middleware component in the pipeline.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes the middleware with the next delegate.
        /// </summary>
        /// <param name="next">Next middleware delegate.</param>
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Executes the middleware logic, wrapping the request pipeline with exception handling.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continue request processing
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log exception message for debugging/monitoring purposes
                Console.WriteLine($"EXCEPTION: {ex.Message}");

                // Ensure response is returned as JSON
                context.Response.ContentType = "application/json";

                // Handle idempotent "already completed" scenario with cached response
                if (ex is AlreadyCompletedException ace)
                {
                    context.Response.StatusCode = 200;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        status = "COMPLETED",
                        response = ace.CachedResponse
                    });

                    return;
                }

                if (ex is ReservationFailedException rfe)
                {
                    context.Response.StatusCode = 409;
                    context.Response.ContentType = "application/json";

                    object? cachedResult;

                    try
                    {
                        cachedResult = System.Text.Json.JsonSerializer
                            .Deserialize<object>(rfe.CachedResponse);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        cachedResult = rfe.CachedResponse;
                    }

                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        code = "RESERVATION_FAILED",
                        message = "This reservation request previously failed.",
                        previousResponse = cachedResult
                    });

                    return;
                }

                // Default error response values
                var statusCode = 500;
                var code = "INTERNAL_ERROR";
                var message = "Something went wrong";

                // Map known exception messages to domain-specific HTTP responses
                switch (ex.Message)
                {
                    case "RESERVATION_FAILED":
                    statusCode = 409;
                    code = "RESERVATION_FAILED";
                    message = "This reservation request previously failed. Check its status for details.";
                    break;

                    case "INVALID_DATE_RANGE":
                        statusCode = 400;
                        code = "INVALID_DATE_RANGE";
                        message = "Check-out date must be later than check-in date";
                        break;

                    case "IDEMPOTENCY_KEY_REQUIRED":
                        statusCode = 400;
                        code = "IDEMPOTENCY_KEY_REQUIRED";
                        message = "An idempotency key is required";
                        break;

                    case "ROOM_CONFLICT":
                        statusCode = 409;
                        code = "ROOM_CONFLICT";
                        message = "Room already booked";
                        break;

                    case "ROOM_LOCKED":
                        statusCode = 409;
                        code = "ROOM_LOCKED";
                        message = "Room is currently locked";
                        break;

                    case "IN_PROGRESS":
                        statusCode = 409;
                        code = "IN_PROGRESS";
                        message = "Request is already being processed";
                        break;

                    case "IDEMPOTENCY_KEY_CONFLICT":
                        statusCode = 409;
                        code = "IDEMPOTENCY_CONFLICT";
                        message = "Same key used with different request";
                        break;

                    case "NOT_FOUND":
                        statusCode = 404;
                        code = "NOT_FOUND";
                        message = "Resource not found";
                        break;
                }

                // Set resolved HTTP status code
                context.Response.StatusCode = statusCode;

                // Return standardized error response payload
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    code,
                    message
                });
            }
        }
    }
}