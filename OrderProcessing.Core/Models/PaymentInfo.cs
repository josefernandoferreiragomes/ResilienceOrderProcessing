namespace OrderProcessing.Core.Models;
public class PaymentInfo
{
    public string PaymentId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}