using OrderProcessing.Api.Mappers;
using OrderProcessing.Core.Models;
using Xunit;

namespace OrderProcessing.Api.Tests.Mappers;

public class OrderMapperTests
{
    private readonly OrderMapper _mapper;

    public OrderMapperTests()
    {
        _mapper = new OrderMapper();
    }

    [Fact]
    public void MapToResponse_WithValidOrder_MapsAllProperties()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = "CUST123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = OrderStatus.Created,
            TotalAmount = 100.00m,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = "PROD1",
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                }
            }
        };

        // Act
        var response = _mapper.MapToResponse(order);

        // Assert
        Assert.Equal(order.Id, response.Id);
        Assert.Equal(order.CustomerId, response.CustomerId);
        Assert.Equal(order.CreatedAt, response.CreatedAt);
        Assert.Equal(order.UpdatedAt, response.UpdatedAt);
        Assert.Equal(order.Status.ToString(), response.Status);
        Assert.Equal(order.TotalAmount, response.TotalAmount);
        Assert.Single(response.Items);
        Assert.Equal(order.Items.First().ProductId, response.Items.First().ProductId);
    }

    [Fact]
    public void MapToResponse_WithShippingAndPayment_MapsAllProperties()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = "CUST123",
            Status = OrderStatus.Processing,
            Payment = new PaymentInfo
            {
                PaymentId = "PAY123",
                PaymentMethod = "Credit Card",
                Status = PaymentStatus.Completed
            },
            Shipping = new ShippingInfo
            {
                TrackingNumber = "TRACK123",
                Carrier = "UPS",
                Status = ShippingStatus.InTransit,
                DeliveryAddress = new Address
                {
                    Street = "123 Test St",
                    City = "Test City",
                    State = "TS",
                    ZipCode = "12345",
                    Country = "Test Country"
                }
            }
        };

        // Act
        var response = _mapper.MapToResponse(order);

        // Assert
        Assert.NotNull(response.Payment);
        Assert.Equal(order.Payment.PaymentId, response.Payment.PaymentId);
        Assert.NotNull(response.Shipping);
        Assert.Equal(order.Shipping.TrackingNumber, response.Shipping.TrackingNumber);
        Assert.Equal(order.Shipping.DeliveryAddress.Street, response.Shipping.DeliveryAddress.Street);
    }
}