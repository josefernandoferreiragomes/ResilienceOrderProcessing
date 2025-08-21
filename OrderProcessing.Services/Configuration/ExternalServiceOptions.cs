namespace OrderProcessing.Services.Configuration;
public class ExternalServiceOptions
{
    public const string SectionName = "ExternalServices";

    public ServiceEndpoint InventoryService { get; set; } = new();
    public ServiceEndpoint PaymentService { get; set; } = new();
    public ServiceEndpoint ShippingService { get; set; } = new();
}

public class ServiceEndpoint
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
    public string ApiKey { get; set; } = string.Empty;
}