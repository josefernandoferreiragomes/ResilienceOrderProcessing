namespace OrderProcessing.Api.Dtos;
public class TestResilienceRequest
{
    public int NumberOfOrders { get; set; } = 10;
    public int DelayBetweenRequestsMs { get; set; } = 100;
}

public class TestResult
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double ProcessingTimeMs { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

public class TestSummary
{
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public List<TestResult> Results { get; set; } = new();
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