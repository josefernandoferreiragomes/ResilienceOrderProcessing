using OrderProcessing.Api.Services;
using System.Diagnostics;

namespace OrderProcessing.Api.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISignalRLoggingService signalRLoggingService)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        var stopwatch = Stopwatch.StartNew();

        var properties = new Dictionary<string, string>
        {
            { "RequestId", requestId },
            { "Method", context.Request.Method },
            { "Path", context.Request.Path },
            { "QueryString", context.Request.QueryString.ToString() },
            { "UserAgent", context.Request.Headers.UserAgent.ToString() },
            { "RemoteIp", context.Connection.RemoteIpAddress?.ToString() ?? "unknown" }
        };

        // Only log to SignalR if it's not a health check or swagger endpoint
        var shouldLog = !IsHealthOrSwaggerEndpoint(context.Request.Path);

        if (shouldLog)
        {
            await signalRLoggingService.LogAsync("Information",
                $"Request started: {context.Request.Method} {context.Request.Path}",
                "OrderProcessing.Api",
                "Request",
                properties);
        }

        try
        {
            await _next(context);
            stopwatch.Stop();

            properties.Add("Duration", stopwatch.ElapsedMilliseconds.ToString());
            properties.Add("StatusCode", context.Response.StatusCode.ToString());

            if (shouldLog)
            {
                var logLevel = context.Response.StatusCode >= 400 ? "Warning" : "Information";

                await signalRLoggingService.LogAsync(logLevel,
                    $"Request completed: {context.Request.Method} {context.Request.Path} - {context.Response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms",
                    "OrderProcessing.Api",
                    "Request",
                    properties);
            }

            _logger.LogInformation("Request {Method} {Path} completed with {StatusCode} in {Duration}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            properties.Add("Duration", stopwatch.ElapsedMilliseconds.ToString());
            properties.Add("ExceptionType", ex.GetType().Name);
            properties.Add("ExceptionMessage", ex.Message);

            // Only include stack trace for non-client errors
            if (context.Response.StatusCode >= 500)
            {
                properties.Add("StackTrace", ex.StackTrace ?? "");
            }

            if (shouldLog)
            {
                await signalRLoggingService.LogAsync("Error",
                    $"Request failed: {context.Request.Method} {context.Request.Path} - {ex.Message}",
                    "OrderProcessing.Api",
                    "Request",
                    properties);
            }

            _logger.LogError(ex, "Request {Method} {Path} failed after {Duration}ms",
                context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private static bool IsHealthOrSwaggerEndpoint(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/swagger") ||
               path.StartsWithSegments("/favicon.ico");
    }
}