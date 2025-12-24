namespace OrderProcessing.Core.Dtos;
public class TestResilienceRequest
{
    public int NumberOfOrders { get; set; } = 10;
    public int DelayBetweenRequestsMs { get; set; } = 100;
}

public class CustomTestResult<T>
{
    public T? ObjectReference { get; set; }
    
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double ProcessingTimeMs { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    
    // Retry metrics
    public int RetryCount { get; set; }
    public double TotalRetryDelayMs { get; set; }
    public List<RetryAttempt> RetryAttempts { get; set; } = new();
    
    public CustomTestResult<V> CloneWithoutObject<V>(CustomTestResult<T> objectToBeCloned)
    {
        return new CustomTestResult<V>
        {
            OrderId = objectToBeCloned.OrderId,
            Status = objectToBeCloned.Status,
            ProcessingTimeMs = objectToBeCloned.ProcessingTimeMs,
            Success = objectToBeCloned.Success,
            FailureReason = objectToBeCloned.FailureReason,
            RetryCount = objectToBeCloned.RetryCount,
            TotalRetryDelayMs = objectToBeCloned.TotalRetryDelayMs,
            RetryAttempts = objectToBeCloned.RetryAttempts
        };
    }
}

public class RetryAttempt
{
    public int AttemptNumber { get; set; }
    public double DelayMs { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class TestSummary<T>
{
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public List<CustomTestResult<T>> Results { get; set; } = new();
    public double SuccessRate => TotalOrders > 0 ? (double)SuccessfulOrders / TotalOrders * 100 : 0;
}

public class SimulateLoadRequest
{
    public int ConcurrentRequests { get; set; } = 20;
}

public class LoadTestResult
{
    public int RequestId { get; set; }
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public double ResponseTimeMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class LoadTestSummary
{
    public int ConcurrentRequests { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public double MinResponseTimeMs { get; set; }
    public double RequestsPerSecond { get; set; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    public List<LoadTestResult> Results { get; set; } = new();
}