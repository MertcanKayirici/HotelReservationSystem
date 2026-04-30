using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HotelReservationSystem.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var method = context.Request.Method;
            var path = context.Request.Path;

            Log.Information("➡️ [REQUEST] {Method} {Path}", method, path);

            await _next(context);

            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;

            Log.Information("⬅️ [RESPONSE] {StatusCode} ({Elapsed} ms)", statusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}