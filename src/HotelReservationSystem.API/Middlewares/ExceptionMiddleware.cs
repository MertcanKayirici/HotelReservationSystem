using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Net;
using System.Text.Json;
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
                context.Response.ContentType = "application/json";

                var statusCode = StatusCodes.Status500InternalServerError;
                var code = "INTERNAL_ERROR";
                var message = "Something went wrong";

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

                if (ex.Message == "ROOM_CONFLICT")
                {
                    statusCode = 409;
                    code = "ROOM_CONFLICT";
                    message = "Room already booked";
                }
                else if (ex.Message == "ROOM_LOCKED")
                {
                    statusCode = 409;
                    code = "ROOM_LOCKED";
                    message = "Room is currently locked";
                }
                else if (ex.Message == "IN_PROGRESS")
                {
                    statusCode = 409;
                    code = "IN_PROGRESS";
                    message = "Request is already being processed";
                }
                else if (ex.Message == "IDEMPOTENCY_KEY_CONFLICT")
                {
                    statusCode = 409;
                    code = "IDEMPOTENCY_CONFLICT";
                    message = "Same key used with different request";
                }
                else if (ex.Message == "NOT_FOUND")
                {
                    context.Response.StatusCode = 404;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        error = "NOT_FOUND"
                    });

                    return;
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