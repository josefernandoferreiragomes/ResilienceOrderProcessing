using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace OrderProcessing.Services.Resilience;

public class ResilienceHealthCheck : IHealthCheck
{
    private readonly ICircuitBreakerMonitor _monitor;
    private readonly ILogger<ResilienceHealthCheck> _logger;

    public ResilienceHealthCheck(ICircuitBreakerMonitor monitor, ILogger<ResilienceHealthCheck> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var allMetrics = _monitor.GetAllMetrics();
            var unhealthyServices = new List<string>();
            var data = new Dictionary<string, object>();

            foreach (var (serviceName, metrics) in allMetrics)
            {
                var isHealthy = IsServiceHealthy(metrics);

                data[$"{serviceName}_state"] = metrics.CurrentState;
                data[$"{serviceName}_success_rate"] = metrics.SuccessRate;
                data[$"{serviceName}_total_retries"] = metrics.TotalRetries;
                data[$"{serviceName}_timeout_count"] = metrics.TimeoutCount;

                if (!isHealthy)
                {
                    unhealthyServices.Add(serviceName);
                }
            }

            if (unhealthyServices.Any())
            {
                var message = $"Unhealthy services: {string.Join(", ", unhealthyServices)}";
                _logger.LogWarning("Resilience health check failed: {Message}", message);
                return Task.FromResult(HealthCheckResult.Degraded(message, data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("All resilience patterns are working correctly", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resilience health check");
            return Task.FromResult(HealthCheckResult.Unhealthy("Error checking resilience health", ex));
        }
    }

    private static bool IsServiceHealthy(CircuitBreakerMetrics metrics)
    {
        // Consider service unhealthy if:
        // 1. Circuit is currently open
        // 2. Success rate is below 70%
        // 3. Too many recent timeouts (more than 5 in recent period)

        if (metrics.CurrentState.Equals("Opened", StringComparison.OrdinalIgnoreCase))
            return false;

        if (metrics.SuccessRate < 70.0 && metrics.TotalRequests > 10)
            return false;

        if (metrics.TimeoutCount > 5 &&
            metrics.LastTimeout.HasValue &&
            metrics.LastTimeout > DateTime.UtcNow.AddMinutes(-5))
            return false;

        return true;
    }
}
