namespace OrderProcessing.Api.Dtos;
public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}