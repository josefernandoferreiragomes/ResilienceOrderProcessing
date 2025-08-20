using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Models;

namespace OrderProcessing.Api.Mappers;
public class OrderMapper
{
    public OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            FailureReason = order.FailureReason,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList(),
            Payment = order.Payment == null ? null : new PaymentInfoResponse
            {
                PaymentId = order.Payment.PaymentId,
                PaymentMethod = order.Payment.PaymentMethod,
                Status = order.Payment.Status.ToString(),
                ProcessedAt = order.Payment.ProcessedAt,
                FailureReason = order.Payment.FailureReason
            },
            Shipping = order.Shipping == null ? null : new ShippingInfoResponse
            {
                TrackingNumber = order.Shipping.TrackingNumber,
                Carrier = order.Shipping.Carrier,
                Status = order.Shipping.Status.ToString(),
                ShippedAt = order.Shipping.ShippedAt,
                EstimatedDelivery = order.Shipping.EstimatedDelivery,
                DeliveryAddress = new AddressResponse
                {
                    Street = order.Shipping.DeliveryAddress.Street,
                    City = order.Shipping.DeliveryAddress.City,
                    State = order.Shipping.DeliveryAddress.State,
                    ZipCode = order.Shipping.DeliveryAddress.ZipCode,
                    Country = order.Shipping.DeliveryAddress.Country
                }
            }
        };
    }

}

