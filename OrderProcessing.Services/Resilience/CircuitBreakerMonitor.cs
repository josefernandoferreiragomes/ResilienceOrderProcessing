using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace OrderProcessing.Services.Resilience;

public interface ICircuitBreakerMonitor
{
    void RecordCircuitBreakerState(string serviceName, string state);
    void RecordRetryAttempt(string serviceName, int attemptNumber, string reason);
    void RecordTimeout(string serviceName, TimeSpan duration);
    CircuitBreakerMetrics GetMetrics(string serviceName);
    Dictionary<string, CircuitBreakerMetrics> GetAllMetrics();
}

public class CircuitBreakerMonitor : ICircuitBreakerMonitor
{
    private readonly ConcurrentDictionary<string, CircuitBreakerMetrics> _metrics = new();
    private readonly ILogger<CircuitBreakerMonitor> _logger;

    public CircuitBreakerMonitor(ILogger<CircuitBreakerMonitor> logger)
    {
        _logger = logger;
    }

    public void RecordCircuitBreakerState(string serviceName, string state)
    {
        var metrics = _metrics.GetOrAdd(serviceName, _ => new CircuitBreakerMetrics { ServiceName = serviceName });

        metrics.LastStateChange = DateTime.UtcNow;
        metrics.CurrentState = state;

        switch (state.ToLower())
        {
            case "opened":
                metrics.CircuitOpenedCount++;
                break;
            case "closed":
                metrics.CircuitClosedCount++;
                break;
            case "half-opened":
                metrics.CircuitHalfOpenedCount++;
                break;
        }

        _logger.LogInformation("Circuit breaker state changed for {ServiceName}: {State}", serviceName, state);
    }

    public void RecordRetryAttempt(string serviceName, int attemptNumber, string reason)
    {
        var metrics = _metrics.GetOrAdd(serviceName, _ => new CircuitBreakerMetrics { ServiceName = serviceName });

        metrics.TotalRetries++;
        metrics.LastRetryAttempt = DateTime.UtcNow;
        metrics.LastRetryReason = reason;

        _logger.LogDebug("Retry attempt {AttemptNumber} for {ServiceName}: {Reason}", attemptNumber, serviceName, reason);
    }

    public void RecordTimeout(string serviceName, TimeSpan duration)
    {
        var metrics = _metrics.GetOrAdd(serviceName, _ => new CircuitBreakerMetrics { ServiceName = serviceName });

        metrics.TimeoutCount++;
        metrics.LastTimeout = DateTime.UtcNow;
        metrics.AverageTimeoutDuration = TimeSpan.FromMilliseconds(
            (metrics.AverageTimeoutDuration.TotalMilliseconds + duration.TotalMilliseconds) / 2);

        _logger.LogWarning("Timeout recorded for {ServiceName}: {Duration}ms", serviceName, duration.TotalMilliseconds);
    }

    public CircuitBreakerMetrics GetMetrics(string serviceName)
    {
        return _metrics.GetOrAdd(serviceName, _ => new CircuitBreakerMetrics { ServiceName = serviceName });
    }

    public Dictionary<string, CircuitBreakerMetrics> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}

public class CircuitBreakerMetrics
{
    public string ServiceName { get; set; } = string.Empty;
    public string CurrentState { get; set; } = "Closed";
    public DateTime? LastStateChange { get; set; }

    // Circuit Breaker Stats
    public int CircuitOpenedCount { get; set; }
    public int CircuitClosedCount { get; set; }
    public int CircuitHalfOpenedCount { get; set; }

    // Retry Stats
    public int TotalRetries { get; set; }
    public DateTime? LastRetryAttempt { get; set; }
    public string? LastRetryReason { get; set; }

    // Timeout Stats
    public int TimeoutCount { get; set; }
    public DateTime? LastTimeout { get; set; }
    public TimeSpan AverageTimeoutDuration { get; set; }

    // Health Stats
    public double SuccessRate { get; set; } = 100.0;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
}
