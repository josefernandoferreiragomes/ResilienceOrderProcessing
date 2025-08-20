namespace OrderProcessing.Core.Models;

public class ShippingInfo
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public ShippingStatus Status { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public Address DeliveryAddress { get; set; } = new();
}


public enum ShippingStatus
{
    Pending,
    Processing,
    Shipped,
    InTransit,
    Delivered,
    Failed
}
