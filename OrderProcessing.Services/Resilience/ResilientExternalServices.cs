using Microsoft.Extensions.Logging;
using OrderProcessing.Core.Dtos;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Diagnostics;

namespace OrderProcessing.Services.Resilience;

public class ResilientInventoryService : IInventoryService
{
    private readonly IInventoryService _innerService;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ResilientInventoryService> _logger;

    public ResilientInventoryService(
        IInventoryService innerService,
        IResiliencePipelineFactory pipelineFactory,
        ILogger<ResilientInventoryService> logger)
    {
        _innerService = innerService;
        _pipeline = pipelineFactory.CreateInventoryPipeline();
        _logger = logger;
        _logger.LogInformation("ResilientInventoryService created, wrapping {InnerType}", innerService.GetType().Name);
    }

    public async Task<CustomTestResult<AvailabilityResponse>> CheckAvailabilityAsync(string productId, int quantity, Guid orderId)
    {
        _logger.LogInformation("CheckAvailabilityAsync started for ProductId={ProductId}, Quantity={Quantity}, OrderId={OrderId}", productId, quantity, orderId);

        var result = new CustomTestResult<AvailabilityResponse> { OrderId = orderId };
        var retryAttempts = new List<RetryAttempt>();
        var stopwatch = Stopwatch.StartNew();

        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(ResilienceContextKeys.RetryAttemptsKey, retryAttempts);

        try
        {
            var innerTestResult = await _pipeline.ExecuteAsync(async (cancellationToken) =>
            {
                _logger.LogDebug("Calling inner CheckAvailabilityAsync for ProductId={ProductId}", productId);
                return await _innerService.CheckAvailabilityAsync(productId, quantity, orderId);
            }, context);

            result.Success = innerTestResult.Success;
            result.Status = innerTestResult.Success ? "Available" : "Unavailable";
            _logger.LogInformation("CheckAvailabilityAsync completed for ProductId={ProductId} with Status={Status}", productId, result.Status);

            return result;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for inventory service. Using fallback for ProductId={ProductId}", productId);
            return await FallbackCheckAvailability(productId, quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckAvailabilityAsync for ProductId={ProductId}: {Message}", productId, ex.Message);
            result.Success = false;
            result.Status = "Failed";
            result.FailureReason = ex.Message;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.RetryAttempts = retryAttempts;
            result.RetryCount = retryAttempts.Count;
            result.TotalRetryDelayMs = retryAttempts.Sum(a => a.DelayMs);
            ResilienceContextPool.Shared.Return(context);

            _logger.LogInformation("CheckAvailabilityAsync finished for ProductId={ProductId}. ProcessingTimeMs={ProcessingTimeMs}, RetryCount={RetryCount}", productId, result.ProcessingTimeMs, result.RetryCount);
        }
    }

    public async Task<bool> ReserveInventoryAsync(string productId, int quantity)
    {
        _logger.LogInformation("ReserveInventoryAsync started for ProductId={ProductId}, Quantity={Quantity}", productId, quantity);
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogDebug("Calling inner ReserveInventoryAsync for ProductId={ProductId}", productId);
                return await _innerService.ReserveInventoryAsync(productId, quantity);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for inventory service. Cannot reserve inventory for ProductId={ProductId}", productId);
            throw new InvalidOperationException("Inventory service is currently unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReserveInventoryAsync for ProductId={ProductId}: {Message}", productId, ex.Message);
            throw;
        }
    }

    public async Task<bool> ReleaseInventoryAsync(string productId, int quantity)
    {
        _logger.LogInformation("ReleaseInventoryAsync started for ProductId={ProductId}, Quantity={Quantity}", productId, quantity);
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogDebug("Calling inner ReleaseInventoryAsync for ProductId={ProductId}", productId);
                return await _innerService.ReleaseInventoryAsync(productId, quantity);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for inventory service. Cannot release inventory for ProductId={ProductId}", productId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReleaseInventoryAsync for ProductId={ProductId}: {Message}", productId, ex.Message);
            throw;
        }
    }

    private Task<CustomTestResult<AvailabilityResponse>> FallbackCheckAvailability(string productId, int quantity)
    {
        var customTestResult = new CustomTestResult<AvailabilityResponse>();
        customTestResult.ObjectReference = new AvailabilityResponse();

        // Simple fallback logic - assume common products are available
        var commonProducts = new[] { "LAPTOP-001", "MOUSE-001", "KEYBOARD-001" };
        var isCommonProduct = commonProducts.Contains(productId);
        var isReasonableQuantity = quantity <= 5;

        customTestResult.ObjectReference.IsAvailable = isCommonProduct && isReasonableQuantity;
         
        _logger.LogInformation("Fallback availability check for {ProductId}: {Available}", productId, customTestResult.Success);

        return Task.FromResult(customTestResult);
    }

    //public Task<CustomTestResult<AvailabilityResponse>> CheckAvailabilityAsyncInner(string productId, int quantity, Guid orderId)
    //{
    //    throw new NotImplementedException();
    //}
}

public class ResilientPaymentService : IPaymentService
{
    private readonly IPaymentService _innerService;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ResilientPaymentService> _logger;

    public ResilientPaymentService(
        IPaymentService innerService,
        IResiliencePipelineFactory pipelineFactory,
        ILogger<ResilientPaymentService> logger)
    {
        _innerService = innerService;
        _pipeline = pipelineFactory.CreatePaymentPipeline();
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogDebug("Executing payment processing for order {OrderId}", request.OrderId);
                return await _innerService.ProcessPaymentAsync(request);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker open for payment service. Payment failed for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                IsSuccess = false,
                PaymentId = Guid.NewGuid().ToString(),
                Status = PaymentStatus.Failed,
                FailureReason = "Payment service is currently unavailable. Please try again later.",
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PaymentResult> GetPaymentStatusAsync(string paymentId)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _innerService.GetPaymentStatusAsync(paymentId);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for payment service. Cannot get status for payment {PaymentId}", paymentId);
            throw new InvalidOperationException("Payment service is currently unavailable");
        }
    }

    public async Task<bool> RefundPaymentAsync(string paymentId, decimal amount)
    {
        try
        {
            var refundPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2, // Fewer retries for refunds
                    Delay = TimeSpan.FromSeconds(2)
                })
                .Build();

            return await refundPipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _innerService.RefundPaymentAsync(paymentId, amount);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker open for payment service. Cannot process refund for payment {PaymentId}", paymentId);
            return false;
        }
    }
}

public class ResilientShippingService : IShippingService
{
    private readonly IShippingService _innerService;
    private readonly ResiliencePipeline _quotePipeline;
    private readonly ResiliencePipeline _shipmentPipeline;
    private readonly ILogger<ResilientShippingService> _logger;

    public ResilientShippingService(
        IShippingService innerService,
        IResiliencePipelineFactory pipelineFactory,
        ILogger<ResilientShippingService> logger)
    {
        _innerService = innerService;
        _quotePipeline = pipelineFactory.CreateShippingPipeline();
        _shipmentPipeline = pipelineFactory.CreateShippingPipeline();
        _logger = logger;
    }

    public async Task<ShippingQuote> GetShippingQuoteAsync(ShippingQuoteRequest request)
    {
        try
        {
            return await _quotePipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _innerService.GetShippingQuoteAsync(request);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for shipping service. Using fallback quote");
            return new ShippingQuote
            {
                Cost = 9.99m, // Default shipping cost
                Carrier = "Standard Carrier",
                ServiceLevel = request.ServiceLevel,
                EstimatedDelivery = DateTime.UtcNow.AddDays(7)
            };
        }
    }

    public async Task<ShippingResult> CreateShipmentAsync(ShippingCreateRequest request)
    {
        try
        {
            return await _shipmentPipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _innerService.CreateShipmentAsync(request);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker open for shipping service. Cannot create shipment for order {OrderId}", request.OrderId);
            return new ShippingResult
            {
                IsSuccess = false,
                TrackingNumber = string.Empty,
                Carrier = string.Empty,
                FailureReason = "Shipping service is currently unavailable. Shipment will be processed later."
            };
        }
    }

    public async Task<ShippingStatus> TrackShipmentAsync(string trackingNumber)
    {
        try
        {
            var trackingPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 2 })
                .Build();

            return await trackingPipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _innerService.TrackShipmentAsync(trackingNumber);
            });
        }
        catch (Exception)
        {
            _logger.LogWarning("Failed to track shipment {TrackingNumber}. Returning unknown status", trackingNumber);
            return ShippingStatus.Processing; // Fallback status
        }
    }
}

public static class ResilienceContextKeys
{
    public static readonly ResiliencePropertyKey<List<RetryAttempt>> RetryAttemptsKey =
        new ResiliencePropertyKey<List<RetryAttempt>>("RetryAttempts");
}