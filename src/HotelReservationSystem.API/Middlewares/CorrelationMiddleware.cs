using Serilog.Context;
using System.Diagnostics;

/// <summary>
/// Middleware responsible for managing correlation IDs across the request pipeline.
/// Ensures that each request has a unique identifier for tracing and logging purposes.
/// </summary>
public class CorrelationMiddleware
{
    /// <summary>
    /// Reference to the next middleware component in the pipeline.
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes the middleware with the next delegate in the pipeline.
    /// </summary>
    /// <param name="next">Next middleware delegate.</param>
    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the incoming HTTP request, assigns or generates a correlation ID,
    /// and enriches the logging context for end-to-end traceability.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    public async Task Invoke(HttpContext context)
    {
        // محاولة الحصول على Correlation ID من الهيدر، وإذا لم يكن موجودًا يتم إنشاء واحد جديد
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Store correlation ID in HttpContext for downstream access (e.g., controllers, services)
        context.Items["CorrelationId"] = correlationId;

        // Include correlation ID in response headers for client-side tracing
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Push correlation ID into Serilog context so it is automatically included in all logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Continue processing the request pipeline
            await _next(context);
        }
    }
}