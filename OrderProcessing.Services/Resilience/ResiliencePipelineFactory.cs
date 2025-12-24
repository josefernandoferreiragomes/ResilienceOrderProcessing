using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Core.Dtos;
using OrderProcessing.Services.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace OrderProcessing.Services.Resilience;

public interface IResiliencePipelineFactory
{
    ResiliencePipeline CreateInventoryPipeline();
    ResiliencePipeline CreatePaymentPipeline();
    ResiliencePipeline CreateShippingPipeline();
}

public class ResiliencePipelineFactory : IResiliencePipelineFactory
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResiliencePipelineFactory> _logger;

    public ResiliencePipelineFactory(IOptions<ResilienceOptions> options, ILogger<ResiliencePipelineFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ResiliencePipeline CreateInventoryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(_options.Timeout.InventoryTimeout)
            .AddRetry(CreateRetryStrategyOptions("Inventory"))
            .AddCircuitBreaker(CreateCircuitBreakerOptions("Inventory"))
            .Build();
    }

    public ResiliencePipeline CreatePaymentPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(_options.Timeout.PaymentTimeout)
            .AddRetry(CreateRetryStrategyOptions("Payment"))
            .AddCircuitBreaker(CreateCircuitBreakerOptions("Payment"))
            .Build();
    }

    public ResiliencePipeline CreateShippingPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(_options.Timeout.ShippingTimeout)
            .AddRetry(CreateRetryStrategyOptions("Shipping"))
            .AddCircuitBreaker(CreateCircuitBreakerOptions("Shipping"))
            .Build();
    }

    private RetryStrategyOptions CreateRetryStrategyOptions(string serviceName)
    {
        return new RetryStrategyOptions
        {
            MaxRetryAttempts = _options.RetryPolicy.MaxRetries,
            Delay = TimeSpan.FromMilliseconds(_options.RetryPolicy.BaseDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(_options.RetryPolicy.MaxDelayMs),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>()
                .Handle<TimeoutRejectedException>()
                .Handle<InvalidOperationException>(ex =>
                    ex.Message.Contains("temporarily unavailable") ||
                    ex.Message.Contains("timeout") ||
                    ex.Message.Contains("service down")),
            OnRetry = args =>
            {
                _logger.LogWarning(
                    "{ServiceName} service retry {AttemptNumber} of {MaxAttempts}. Delay: {Delay}ms. Exception: {Exception}",
                    serviceName,
                    args.AttemptNumber,
                    _options.RetryPolicy.MaxRetries,
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.Message ?? "Unknown");

                // Add retry attempt to the context if available
                if (args.Context.Properties.TryGetValue(ResilienceContextKeys.RetryAttemptsKey, out var retryAttempts))
                {
                    retryAttempts.Add(new RetryAttempt
                    {
                        AttemptNumber = args.AttemptNumber,
                        DelayMs = args.RetryDelay.TotalMilliseconds,
                        FailureReason = args.Outcome.Exception?.Message,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }

                return ValueTask.CompletedTask;
            }
        };
    }

    private CircuitBreakerStrategyOptions CreateCircuitBreakerOptions(string serviceName)
    {
        return new CircuitBreakerStrategyOptions
        {
            FailureRatio = (double)_options.CircuitBreaker.FailureThreshold / _options.CircuitBreaker.MinimumThroughput,
            SamplingDuration = _options.CircuitBreaker.SamplingDuration,
            MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
            BreakDuration = _options.CircuitBreaker.BreakDuration,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>()
                .Handle<TimeoutRejectedException>()
                .Handle<InvalidOperationException>(),
            OnOpened = args =>
            {
                _logger.LogError(
                    "{ServiceName} circuit breaker opened. Duration: {Duration}ms",
                    serviceName,
                    args.BreakDuration.TotalMilliseconds);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                _logger.LogInformation("{ServiceName} circuit breaker closed", serviceName);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                _logger.LogInformation("{ServiceName} circuit breaker half-opened", serviceName);
                return ValueTask.CompletedTask;
            }
        };
    }
}

