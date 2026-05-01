using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;
using static HotelReservationSystem.Application.Services.ReservationService;

namespace HotelReservationSystem.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION: {ex.Message}");

                context.Response.ContentType = "application/json";

                // 🔥 1. COMPLETED (cached response)
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

                // 🔥 2. DEFAULT
                var statusCode = 500;
                var code = "INTERNAL_ERROR";
                var message = "Something went wrong";

                switch (ex.Message)
                {
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

                context.Response.StatusCode = statusCode;

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