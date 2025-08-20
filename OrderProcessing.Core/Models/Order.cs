namespace OrderProcessing.Core.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public PaymentInfo? Payment { get; set; }
    public ShippingInfo? Shipping { get; set; }
    public string? FailureReason { get; set; }
}
