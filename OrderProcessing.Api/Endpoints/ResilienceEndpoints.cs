using OrderProcessing.Services.Resilience;

namespace OrderProcessing.Api.Endpoints;
public static class ResilienceEndpoints
{
    public static IEndpointRouteBuilder MapResilienceMonitoringEndpoints(this IEndpointRouteBuilder endpoints)
    {


        // Get circuit breaker status for all services
        endpoints.MapGet("/circuit-breakers", (ICircuitBreakerMonitor monitor) =>
        {
            var metrics = monitor.GetAllMetrics();
            return Results.Ok(metrics);
        })
        .WithName("GetCircuitBreakerStatus")
        .WithSummary("Get circuit breaker status for all services")
        .Produces<Dictionary<string, CircuitBreakerMetrics>>(200);

        // Get circuit breaker status for specific service
        endpoints.MapGet("/circuit-breakers/{serviceName}", (string serviceName, ICircuitBreakerMonitor monitor) =>
        {
            var metrics = monitor.GetMetrics(serviceName);
            return Results.Ok(metrics);
        })
        .WithName("GetServiceCircuitBreakerStatus")
        .WithSummary("Get circuit breaker status for a specific service")
        .Produces<CircuitBreakerMetrics>(200);

        // Resilience summary endpoint
        endpoints.MapGet("/resilience-summary", (ICircuitBreakerMonitor monitor) =>
        {
            var allMetrics = monitor.GetAllMetrics();

            var summary = new
            {
                TotalServices = allMetrics.Count,
                HealthyServices = allMetrics.Count(m => m.Value.CurrentState == "Closed"),
                OpenCircuits = allMetrics.Count(m => m.Value.CurrentState == "Opened"),
                TotalRetries = allMetrics.Sum(m => m.Value.TotalRetries),
                TotalTimeouts = allMetrics.Sum(m => m.Value.TimeoutCount),
                AverageSuccessRate = allMetrics.Any() ? allMetrics.Average(m => m.Value.SuccessRate) : 100.0,
                LastUpdated = DateTime.UtcNow
            };

            return Results.Ok(summary);
        })
        .WithName("GetResilienceSummary")
        .WithSummary("Get overall resilience summary")
        .Produces(200);

        return endpoints;
    }
}

