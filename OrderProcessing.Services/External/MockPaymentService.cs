using Microsoft.Extensions.Logging;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;
    private readonly Random _random = new();
    private readonly Dictionary<string, PaymentResult> _payments = new();

    public MockPaymentService(ILogger<MockPaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        _logger.LogInformation("Processing payment for order {OrderId}, amount {Amount}",
            request.OrderId, request.Amount);

        // Simulate network delay
        await Task.Delay(_random.Next(1000, 3000));

        var paymentId = Guid.NewGuid().ToString();

        // Simulate different scenarios
        var scenario = _random.Next(1, 101);
        PaymentResult result;

        if (scenario <= 15) // 15% chance of failure
        {
            result = new PaymentResult
            {
                IsSuccess = false,
                PaymentId = paymentId,
                Status = PaymentStatus.Failed,
                FailureReason = GetRandomFailureReason(),
                ProcessedAt = DateTime.UtcNow
            };
            _logger.LogWarning("Payment failed for order {OrderId}: {Reason}",
                request.OrderId, result.FailureReason);
        }
        else if (scenario <= 25) // 10% chance of timeout/error
        {
            _logger.LogError("Payment service timeout for order {OrderId}", request.OrderId);
            throw new TimeoutException("Payment service timeout");
        }
        else // 75% chance of success
        {
            result = new PaymentResult
            {
                IsSuccess = true,
                PaymentId = paymentId,
                Status = PaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow
            };
            _logger.LogInformation("Payment completed successfully for order {OrderId}", request.OrderId);
        }

        _payments[paymentId] = result;
        return result;
    }

    public async Task<PaymentResult> GetPaymentStatusAsync(string paymentId)
    {
        _logger.LogInformation("Retrieving payment status for payment {PaymentId}", paymentId);

        // Simulate network delay
        await Task.Delay(_random.Next(100, 500));

        if (_payments.TryGetValue(paymentId, out var payment))
        {
            return payment;
        }

        throw new ArgumentException($"Payment {paymentId} not found");
    }

    public async Task<bool> RefundPaymentAsync(string paymentId, decimal amount)
    {
        _logger.LogInformation("Processing refund for payment {PaymentId}, amount {Amount}", paymentId, amount);

        // Simulate network delay
        await Task.Delay(_random.Next(500, 1500));

        if (_payments.TryGetValue(paymentId, out var payment) && payment.IsSuccess)
        {
            payment.Status = PaymentStatus.Refunded;
            _logger.LogInformation("Refund processed successfully for payment {PaymentId}", paymentId);
            return true;
        }

        _logger.LogWarning("Failed to process refund for payment {PaymentId}", paymentId);
        return false;
    }

    private string GetRandomFailureReason()
    {
        var reasons = new[]
        {
            "Insufficient funds",
            "Card declined",
            "Invalid card number",
            "Card expired",
            "Payment processor unavailable",
            "Fraud detected"
        };

        return reasons[_random.Next(reasons.Length)];
    }
}
