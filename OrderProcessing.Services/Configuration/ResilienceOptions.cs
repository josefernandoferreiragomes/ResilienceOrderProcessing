namespace OrderProcessing.Services.Configuration;

public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public RetryPolicyOptions RetryPolicy { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
    public BulkheadOptions Bulkhead { get; set; } = new();
}

public class RetryPolicyOptions
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 10000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public double Jitter { get; set; } = 0.1;
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 3;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);
    public int MinimumThroughput { get; set; } = 3;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}

public class TimeoutOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan InventoryTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan PaymentTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan ShippingTimeout { get; set; } = TimeSpan.FromSeconds(8);
}

public class BulkheadOptions
{
    public int MaxConcurrentInventory { get; set; } = 10;
    public int MaxConcurrentPayment { get; set; } = 5;
    public int MaxConcurrentShipping { get; set; } = 8;
    public int MaxQueueLength { get; set; } = 25;
}
