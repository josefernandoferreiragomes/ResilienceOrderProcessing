using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Api.Dtos;
using OrderProcessing.Api.Mappers;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;

namespace OrderProcessing.Api.Endpoints;
public static class OrderEndpointsBuilder
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {

        // Create new order
        endpoints.MapPost("/", async (CreateOrderRequest request, IOrderService orderService, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("Creating new order for customer {CustomerId}", request.CustomerId);

                var order = await orderService.CreateOrderAsync(request);
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var response = orderMapper.MapToResponse(order);

                return Results.CreatedAtRoute("GetOrderById", new { id = order.Id }, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateOrder")
        .WithSummary("Create a new order")
        .Produces<OrderResponse>(201)
        .Produces(400);

        // Get order by ID
        endpoints.MapGet("/{id:guid}", async (Guid id, IOrderService orderService, ILogger<Program> logger) =>
        {
            try
            {
                var order = await orderService.GetOrderAsync(id);
                if (order == null)
                {
                    return Results.NotFound(new { error = $"Order {id} not found" });
                }
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                return Results.Ok(orderMapper.MapToResponse(order));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve order {OrderId}", id);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetOrderById")
        .WithSummary("Get order by ID")
        .Produces<OrderResponse>(200)
        .Produces(404)
        .Produces(400);

        // Get customer orders
        endpoints.MapGet("/customer/{customerId}", async (string customerId, IOrderService orderService, ILogger<Program> logger) =>
        {
            try
            {
                var customerOrders = await orderService.GetCustomerOrdersAsync(customerId);
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var responses = customerOrders.Select(orderMapper.MapToResponse);

                return Results.Ok(responses);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve orders for customer {CustomerId}", customerId);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetCustomerOrders")
        .WithSummary("Get all orders for a customer")
        .Produces<IEnumerable<OrderResponse>>(200)
        .Produces(400);

        // Process order
        endpoints.MapPost("/{id:guid}/process", async (Guid id, IOrderService orderService, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("Processing order {OrderId}", id);

                var order = await orderService.ProcessOrderAsync(id);
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var response = orderMapper.MapToResponse(order);

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process order {OrderId}", id);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ProcessOrder")
        .WithSummary("Process an order through the complete workflow")
        .Produces<OrderResponse>(200)
        .Produces(404)
        .Produces(400);

        // Update order status
        endpoints.MapPut("/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest request, IOrderService orderService, ILogger<Program> logger) =>
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
                {
                    return Results.BadRequest(new { error = "Invalid order status" });
                }

                var order = await orderService.UpdateOrderStatusAsync(id, status, request.FailureReason);
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var response = orderMapper.MapToResponse(order);

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update status for order {OrderId}", id);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateOrderStatus")
        .WithSummary("Update order status")
        .Produces<OrderResponse>(200)
        .Produces(404)
        .Produces(400);

        // Get all orders (for demo purposes)
        endpoints.MapGet("/", async (IOrderRepository orderRepository, ILogger<Program> logger) =>
        {
            try
            {
                var allOrders = await orderRepository.GetAllAsync();
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var responses = allOrders.Select(orderMapper.MapToResponse);

                return Results.Ok(responses);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve all orders");
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetAllOrders")
        .WithSummary("Get all orders (demo endpoint)")
        .Produces<IEnumerable<OrderResponse>>(200)
        .Produces(400);

        // Get Next page of orders
        endpoints.MapGet("/page/{page:int}", async (int page, IOrderService orderService, ILogger<Program> logger, [FromQuery] int pageSize = 10) =>
        {
            try
            {
                var allOrders = await orderService.GetOrdersNextPageAsync(page,pageSize);
                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var responses = allOrders.Select(orderMapper.MapToResponse);

                return Results.Ok(responses);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve next page of orders");
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetOrdersNextPageAsync")
        .WithSummary("Get next page of orders")
        .Produces<IEnumerable<OrderResponse>>(200)
        .Produces(400);

        return endpoints;
    }
}
