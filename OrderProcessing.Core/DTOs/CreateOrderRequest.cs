namespace OrderProcessing.Core.DTOs;

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public List<CreateOrderItemRequest> Items { get; set; } = new();
    public PaymentMethodRequest PaymentMethod { get; set; } = new();
    public AddressRequest DeliveryAddress { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PaymentMethodRequest
{
    public string Method { get; set; } = string.Empty; // "CreditCard", "PayPal", etc.
    public string Token { get; set; } = string.Empty;
}

public class AddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

