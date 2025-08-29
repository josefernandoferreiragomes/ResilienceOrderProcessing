using LoggingApi.Models;
using LoggingApi.Hubs;
using LoggingApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LoggingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoggingController : ControllerBase
{
    private readonly IHubContext<LoggingHub> _hubContext;
    private readonly IFeatureToggleService _featureToggleService;

    public LoggingController(IHubContext<LoggingHub> hubContext, IFeatureToggleService featureToggleService)
    {
        _hubContext = hubContext;
        _featureToggleService = featureToggleService;
    }

    [HttpPost("log")]
    public async Task<IActionResult> LogMessage([FromBody] LogMessage logMessage)
    {
        if (!_featureToggleService.IsFeatureEnabled("RealTimeLogging"))
        {
            return Ok("Real-time logging is disabled");
        }

        // Enhanced log message with timestamp
        var enhancedLog = new
        {
            logMessage.Level,
            logMessage.Message,
            logMessage.Source,
            logMessage.Category,
            Timestamp = DateTime.UtcNow,
            logMessage.Properties
        };

        // Send to all clients in the logging group
        await _hubContext.Clients.Group("LoggingGroup").SendAsync("ReceiveLog", enhancedLog);

        // If detailed error logging is enabled, send additional context for errors
        if (logMessage.Level == "Error" && _featureToggleService.IsFeatureEnabled("DetailedErrorLogging"))
        {
            var detailedLog = new
            {
                Type = "DetailedError",
                enhancedLog.Level,
                enhancedLog.Message,
                enhancedLog.Source,
                enhancedLog.Category,
                enhancedLog.Timestamp,
                StackTrace = logMessage.Properties?.GetValueOrDefault("StackTrace"),
                RequestId = logMessage.Properties?.GetValueOrDefault("RequestId"),
                UserId = logMessage.Properties?.GetValueOrDefault("UserId")
            };

            await _hubContext.Clients.Group("ErrorLoggingGroup").SendAsync("ReceiveDetailedError", detailedLog);
        }

        return Ok("Log message sent successfully");
    }

    [HttpPost("performance")]
    public async Task<IActionResult> LogPerformance([FromBody] PerformanceLog performanceLog)
    {
        if (!_featureToggleService.IsFeatureEnabled("PerformanceLogging"))
        {
            return Ok("Performance logging is disabled");
        }

        var enhancedPerformanceLog = new
        {
            performanceLog.Operation,
            performanceLog.Duration,
            performanceLog.Source,
            Timestamp = DateTime.UtcNow,
            performanceLog.Metadata
        };

        await _hubContext.Clients.Group("PerformanceGroup").SendAsync("ReceivePerformanceLog", enhancedPerformanceLog);

        return Ok("Performance log sent successfully");
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}

