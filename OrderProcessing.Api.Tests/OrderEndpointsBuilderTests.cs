using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using OrderProcessing.Api.Dtos;
using OrderProcessing.Api.Endpoints;
using OrderProcessing.Api.Mappers;
using OrderProcessing.Api.Tests.Helpers;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using Xunit;

namespace OrderProcessing.Api.Tests.Endpoints;

public class OrderEndpointsBuilderTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<ILogger<Program>> _loggerMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly OrderMapper _orderMapper;

    public OrderEndpointsBuilderTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _loggerMock = new Mock<ILogger<Program>>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _orderMapper = new OrderMapper();
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedResponse()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST123"
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Created
        };

        _orderServiceMock
            .Setup(x => x.CreateOrderAsync(It.IsAny<CreateOrderRequest>()))
            .ReturnsAsync(order);

        // Act
        var result = await CreateOrderEndpoint(request);

        // Assert
        var createdResult = Assert.IsType<Created<OrderResponse>>(result);
        Assert.Equal(order.Id, createdResult.Value!.Id);
        Assert.Equal(order.CustomerId, createdResult.Value.CustomerId);
    }

    [Fact]
    public async Task GetOrderById_WithExistingOrder_ReturnsOkResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = "CUST123",
            Status = OrderStatus.Created
        };

        _orderServiceMock
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await GetOrderByIdEndpoint(orderId);

        // Assert
        var okResult = Assert.IsType<Ok<OrderResponse>>(result);
        Assert.Equal(order.Id, okResult.Value!.Id);
        Assert.Equal(order.CustomerId, okResult.Value.CustomerId);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidStatus_ReturnsOkResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest
        {
            Status = "Processing"
        };

        var order = new Order
        {
            Id = orderId,
            CustomerId = "CUST123",
            Status = OrderStatus.Processing
        };

        _orderServiceMock
            .Setup(x => x.UpdateOrderStatusAsync(
                orderId,
                OrderStatus.Processing,
                It.IsAny<string>()))
            .ReturnsAsync(order);

        // Act
        var result = await UpdateOrderStatusEndpoint(orderId, request);

        // Assert
        var okResult = Assert.IsType<Ok<OrderResponse>>(result);
        Assert.Equal(order.Id, okResult.Value!.Id);
        Assert.Equal("Processing", okResult.Value.Status);
    }

    private async Task<IResult> CreateOrderEndpoint(CreateOrderRequest request)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_orderMapper);
        var serviceProvider = services.BuildServiceProvider();

        var endpoints = new TestEndpointRouteBuilder(serviceProvider);
        return await OrderEndpointsBuilder
            .MapOrderEndpoints(endpoints)
            .CreateOrderDelegate(request, _orderServiceMock.Object, _loggerMock.Object);
    }

    private async Task<IResult> GetOrderByIdEndpoint(Guid orderId)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_orderMapper);
        var serviceProvider = services.BuildServiceProvider();

        var endpoints = new TestEndpointRouteBuilder(serviceProvider);
        return await OrderEndpointsBuilder
            .MapOrderEndpoints(endpoints)
            .GetOrderByIdDelegate(orderId, _orderServiceMock.Object, _loggerMock.Object);
    }

    private async Task<IResult> UpdateOrderStatusEndpoint(Guid orderId, UpdateOrderStatusRequest request)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_orderMapper);
        var serviceProvider = services.BuildServiceProvider();

        var endpoints = new TestEndpointRouteBuilder(serviceProvider);
        return await OrderEndpointsBuilder
            .MapOrderEndpoints(endpoints)
            .UpdateOrderStatusDelegate(orderId, request, _orderServiceMock.Object, _loggerMock.Object);
    }
}

