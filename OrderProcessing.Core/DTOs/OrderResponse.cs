namespace OrderProcessing.Core.DTOs;
public class OrderResponse
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public PaymentInfoResponse? Payment { get; set; }
    public ShippingInfoResponse? Shipping { get; set; }
    public string? FailureReason { get; set; }
}

public class OrderItemResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentInfoResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}

public class ShippingInfoResponse
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public AddressResponse DeliveryAddress { get; set; } = new();
}

public class AddressResponse
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}