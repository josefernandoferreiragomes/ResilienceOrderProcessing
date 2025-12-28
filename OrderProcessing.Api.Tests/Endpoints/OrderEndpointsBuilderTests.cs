using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderProcessing.Api.Dtos;
using OrderProcessing.Api.Endpoints;
using OrderProcessing.Api.Mappers;
using OrderProcessing.Api.Services;
using OrderProcessing.Core.Dtos;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrderProcessing.Api.Tests.Endpoints;

public class OrderEndpointsBuilderTests : IAsyncLifetime
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<ISignalRLoggingService> _signalRLoggingServiceMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _httpClient;

    public OrderEndpointsBuilderTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _signalRLoggingServiceMock = new Mock<ISignalRLoggingService>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
    }

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove real implementations
                    var orderServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOrderService));
                    if (orderServiceDescriptor != null)
                        services.Remove(orderServiceDescriptor);

                    var signalRDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISignalRLoggingService));
                    if (signalRDescriptor != null)
                        services.Remove(signalRDescriptor);

                    var repoDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOrderRepository));
                    if (repoDescriptor != null)
                        services.Remove(repoDescriptor);

                    // Add mocks
                    services.AddSingleton(_orderServiceMock.Object);
                    services.AddSingleton(_signalRLoggingServiceMock.Object);
                    services.AddSingleton(_orderRepositoryMock.Object);
                });
            });

        _httpClient = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    #region CreateOrder Tests

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST123",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = "PROD1", ProductName = "Product 1", Quantity = 2, UnitPrice = 99.99m }
            },
            PaymentMethod = new PaymentMethodRequest { Method = "CreditCard", Token = "token123" },
            DeliveryAddress = new AddressRequest
            {
                Street = "123 Main St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345",
                Country = "USA"
            }
        };

        var expectedOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Created,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = 199.98m
        };

        _orderServiceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<CreateOrderRequest>()))
            .ReturnsAsync(expectedOrder);

        _signalRLoggingServiceMock
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        _signalRLoggingServiceMock
            .Setup(x => x.LogPerformanceAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/orders/", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);

        _orderServiceMock.Verify(
            x => x.CreateOrderAsync(It.IsAny<CreateOrderRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WithMissingCustomerId_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = "PROD1", ProductName = "Product 1", Quantity = 1, UnitPrice = 99.99m }
            },
            PaymentMethod = new PaymentMethodRequest { Method = "CreditCard", Token = "token123" },
            DeliveryAddress = new AddressRequest()
        };

        _signalRLoggingServiceMock
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/orders/", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _orderServiceMock.Verify(
            x => x.CreateOrderAsync(It.IsAny<CreateOrderRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WithNoItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST123",
            Items = new List<CreateOrderItemRequest>(),
            PaymentMethod = new PaymentMethodRequest { Method = "CreditCard", Token = "token123" },
            DeliveryAddress = new AddressRequest()
        };

        _signalRLoggingServiceMock
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/orders/", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST123",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = "PROD1", ProductName = "Product 1", Quantity = 1, UnitPrice = 99.99m }
            },
            PaymentMethod = new PaymentMethodRequest { Method = "CreditCard", Token = "token123" },
            DeliveryAddress = new AddressRequest()
        };

        _orderServiceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<CreateOrderRequest>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        _signalRLoggingServiceMock
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        _signalRLoggingServiceMock
            .Setup(x => x.LogPerformanceAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/orders/", request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region GetOrderById Tests

    [Fact]
    public async Task GetOrderById_WithExistingOrder_ReturnsOkWithOrderData()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = "CUST123",
            Status = OrderStatus.Created,
            Items = new List<OrderItem>(),
            TotalAmount = 0
        };

        _orderServiceMock
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        _signalRLoggingServiceMock
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        _signalRLoggingServiceMock
            .Setup(x => x.LogPerformanceAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _httpClient!.GetAsync($"/api/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetOrderById_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderServiceMock
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync((Order?)null);

        _signalRLoggingServiceMock
            .Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _httpClient!.GetAsync($"/api/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_WithInvalidGuid_ReturnsBadRequest()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/orders/invalid-guid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region UpdateOrderStatus Tests

    [Fact]
    public async Task UpdateOrderStatus_WithValidStatus_ReturnsOkWithUpdatedOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest
        {
            Status = OrderStatus.Processing.ToString()
        };

        var updatedOrder = new Order
        {
            Id = orderId,
            CustomerId = "CUST123",
            Status = OrderStatus.Processing,
            Items = new List<OrderItem>(),
            TotalAmount = 0
        };

        _orderServiceMock
            .Setup(x => x.UpdateOrderStatusAsync(orderId, OrderStatus.Processing, null))
            .ReturnsAsync(updatedOrder);

        // Act
        var response = await _httpClient!.PutAsJsonAsync($"/api/orders/{orderId}/status", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest
        {
            Status = "InvalidStatus"
        };

        // Act
        var response = await _httpClient!.PutAsJsonAsync($"/api/orders/{orderId}/status", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _orderServiceMock.Verify(
            x => x.UpdateOrderStatusAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest
        {
            Status = OrderStatus.Processing.ToString()
        };

        _orderServiceMock
            .Setup(x => x.UpdateOrderStatusAsync(orderId, OrderStatus.Processing, null))
            .ThrowsAsync(new ArgumentException($"Order {orderId} not found"));

        // Act
        var response = await _httpClient!.PutAsJsonAsync($"/api/orders/{orderId}/status", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GetAllOrders Tests

    [Fact]
    public async Task GetAllOrders_ReturnsOkWithOrdersList()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), CustomerId = "CUST1", Status = OrderStatus.Created, Items = new(), TotalAmount = 0 },
            new() { Id = Guid.NewGuid(), CustomerId = "CUST2", Status = OrderStatus.Processing, Items = new(), TotalAmount = 0 }
        };

        _orderRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(orders);

        // Act
        var response = await _httpClient!.GetAsync("/api/orders/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<IEnumerable<dynamic>>();
        Assert.NotNull(content);
        Assert.Equal(2, content.Count());
    }

    #endregion

    #region ProcessOrder Tests

    [Fact]
    public async Task ProcessOrder_WithExistingOrder_ReturnsOkWithProcessedOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = "CUST123",
            Status = OrderStatus.Shipped,
            Items = new List<OrderItem>(),
            TotalAmount = 0
        };

        var result = new CustomTestResult<Order> { ObjectReference = order, Success = true };

        _orderServiceMock
            .Setup(x => x.ProcessOrderAsync(orderId))
            .ReturnsAsync(result);

        // Act
        var response = await _httpClient!.PostAsync($"/api/orders/{orderId}/process", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOrder_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderServiceMock
            .Setup(x => x.ProcessOrderAsync(orderId))
            .ThrowsAsync(new ArgumentException($"Order {orderId} not found"));

        // Act
        var response = await _httpClient!.PostAsync($"/api/orders/{orderId}/process", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}