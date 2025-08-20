using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Models;

namespace OrderProcessing.Core.ExternalServices.Models;
public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentToken { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}

public class ShippingQuote
{
    public decimal Cost { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string ServiceLevel { get; set; } = string.Empty;
    public DateTime EstimatedDelivery { get; set; }
}

public class ShippingQuoteRequest
{
    public Address FromAddress { get; set; } = new();
    public Address ToAddress { get; set; } = new();
    public decimal Weight { get; set; }
    public string ServiceLevel { get; set; } = "Standard";
}

public class ShippingResult
{
    public bool IsSuccess { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public DateTime EstimatedDelivery { get; set; }
    public string? FailureReason { get; set; }
}

public class ShippingCreateRequest
{
    public string OrderId { get; set; } = string.Empty;
    public Address FromAddress { get; set; } = new();
    public Address ToAddress { get; set; } = new();
    public decimal Weight { get; set; }
    public string ServiceLevel { get; set; } = "Standard";
    public List<OrderItem> Items { get; set; } = new();
}