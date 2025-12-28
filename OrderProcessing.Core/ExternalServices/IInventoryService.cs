using OrderProcessing.Core.Dtos;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Models;
namespace OrderProcessing.Core.ExternalServices;

public interface IInventoryService
{
    Task<CustomTestResult<AvailabilityResponse>> CheckAvailabilityAsync(string productId, int quantity, Guid orderId);
    //Task<CustomTestResult<AvailabilityResponse>> CheckAvailabilityAsyncInner(string productId, int quantity, Guid orderId);
    Task<bool> ReserveInventoryAsync(string productId, int quantity);
    Task<bool> ReleaseInventoryAsync(string productId, int quantity);
}

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> GetPaymentStatusAsync(string paymentId);
    Task<bool> RefundPaymentAsync(string paymentId, decimal amount);
}

public interface IShippingService
{
    Task<ShippingQuote> GetShippingQuoteAsync(ShippingQuoteRequest request);
    Task<ShippingResult> CreateShipmentAsync(ShippingCreateRequest request);
    Task<ShippingStatus> TrackShipmentAsync(string trackingNumber);
}
