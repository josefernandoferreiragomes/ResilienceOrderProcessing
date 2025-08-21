namespace OrderProcessing.Core.Models;
public class OrderItem
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public enum OrderStatus
{
    Created,
    InventoryChecked,
    PaymentProcessing,
    PaymentCompleted,
    PaymentFailed,
    Shipping,
    Shipped,
    Delivered,
    Cancelled,
    Failed,
    Processing
}