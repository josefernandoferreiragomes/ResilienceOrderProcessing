using Microsoft.Extensions.Logging;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

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
    }

    public async Task<bool> CheckAvailabilityAsync(string productId, int quantity)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogDebug("Executing availability check for product {ProductId}", productId);
                return await _innerService.CheckAvailabilityAsync(productId, quantity);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for inventory service. Using fallback for product {ProductId}", productId);
            return await FallbackCheckAvailability(productId, quantity);
        }
    }

    public async Task<bool> ReserveInventoryAsync(string productId, int quantity)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogDebug("Executing inventory reservation for product {ProductId}", productId);
                return await _innerService.ReserveInventoryAsync(productId, quantity);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for inventory service. Cannot reserve inventory for product {ProductId}", productId);
            throw new InvalidOperationException("Inventory service is currently unavailable");
        }
    }

    public async Task<bool> ReleaseInventoryAsync(string productId, int quantity)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _innerService.ReleaseInventoryAsync(productId, quantity);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker open for inventory service. Cannot release inventory for product {ProductId}", productId);
            // For release operations, we might want to queue this for later processing
            return false;
        }
    }

    private Task<bool> FallbackCheckAvailability(string productId, int quantity)
    {
        // Simple fallback logic - assume common products are available
        var commonProducts = new[] { "LAPTOP-001", "MOUSE-001", "KEYBOARD-001" };
        var isCommonProduct = commonProducts.Contains(productId);
        var isReasonableQuantity = quantity <= 5;

        var result = isCommonProduct && isReasonableQuantity;
        _logger.LogInformation("Fallback availability check for {ProductId}: {Available}", productId, result);

        return Task.FromResult(result);
    }
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