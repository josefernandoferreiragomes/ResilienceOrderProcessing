using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Api.Dtos;
using OrderProcessing.Api.Mappers;
using OrderProcessing.Api.Services;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using Polly;
using System.Diagnostics;

namespace OrderProcessing.Api.Endpoints;
public static class OrderEndpointsBuilder
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {

        // Create new order
        endpoints.MapPost("/", async (
            CreateOrderRequest request, 
            IOrderService orderService, 
            ILogger<Program> logger,
            ISignalRLoggingService signalRLoggingService) =>
        {

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            try
            {
                logger.LogInformation("Creating new order for customer {CustomerId}", request.CustomerId);

                var properties = new Dictionary<string, string>
                {
                    { "RequestId",  requestId},
                    { "CustomerId", request.CustomerId },
                    { "ItemsCount", request.Items.Count.ToString() },
                };

                await signalRLoggingService.LogAsync("Information",
                    $"Processing order creation for customer {request.CustomerId}",
                    "OrderProcessing.Api",
                    "Order",
                    properties);

                // Validate request
                if (string.IsNullOrEmpty(request.CustomerId))
                {
                    await signalRLoggingService.LogAsync("Warning",
                        "Order creation attempted with empty CustomerId",
                        "OrderProcessing.Api",
                        "Validation",
                        properties);

                    return Results.BadRequest(new { Error = "CustomerId is required" });
                }
                if (request.Items.Count <= 0)
                {
                    await signalRLoggingService.LogAsync("Warning",
                        "Order creation attempted with invalid quantity",
                        "OrderProcessing.Api",
                        "Validation",
                        properties);

                    return Results.BadRequest(new { Error = "Quantity must be greater than 0" });
                }

                var order = await orderService.CreateOrderAsync(request);

                stopwatch.Stop();

                // Log performance
                await signalRLoggingService.LogPerformanceAsync("CreateOrder",
                    stopwatch.ElapsedMilliseconds,
                    "OrderProcessing.Api",
                    new Dictionary<string, object>
                    {
                        { "RequestId", requestId },
                        { "Success", true },
                        { "CustomerId", request.CustomerId },
                        { "OrderId", order.Id }
                    });

                await signalRLoggingService.LogAsync("Information",
                    $"Order {order.Id} created successfully for customer {request.CustomerId}",
                    "OrderProcessing.Api",
                    "Order",
                    properties);

                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                var response = orderMapper.MapToResponse(order);

                return Results.Ok(new
                {
                    OrderId = order.Id,
                    Status = "Created",
                    RequestId = requestId,
                    ProcessingTime = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorProperties = new Dictionary<string, string>
                {
                    { "RequestId", requestId},
                    { "ExceptionType", ex.GetType().Name },
                    { "ExceptionMessage", ex.Message },
                    { "StackTrace", ex.StackTrace ?? "" }
                };

                await signalRLoggingService.LogAsync("Error",
                    $"Order creation failed: {ex.Message}",
                    "OrderProcessing.Api",
                    "Order",
                    errorProperties);

                // Log performance for failed operation
                await signalRLoggingService.LogPerformanceAsync("CreateOrder",
                    stopwatch.ElapsedMilliseconds,
                    "OrderProcessing.Api",
                    new Dictionary<string, object>
                    {
                        { "Success", false },
                        { "ExceptionType", ex.GetType().Name },
                        { "ExceptionMessage", ex.Message }
                    });

                logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);

                return Results.Problem(
                   title: "Order creation failed",
                   detail: ex.Message,
                   statusCode: 500);
            }
        })
        .WithName("CreateOrder")
        .WithSummary("Create a new order")
        .WithDescription("Creates a new order with the specified details and returns the order ID")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<object>(StatusCodes.Status500InternalServerError);


        //Get order by ID
        endpoints.MapGet("/{id:guid}", async (
            Guid id, 
            IOrderService orderService, 
            ILogger<Program> logger,
            ISignalRLoggingService signalRLoggingService) =>
        {
            var requestId = Guid.NewGuid().ToString();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var properties = new Dictionary<string, string>
                {
                    { "RequestId", requestId },
                    { "OrderId", id.ToString() }
                };

                await signalRLoggingService.LogAsync("Information",
                    $"Retrieving order {id}",
                    "OrderProcessing.Api",
                    "Order",
                    properties);

                var order = await orderService.GetOrderAsync(id);

                stopwatch.Stop();

                if (order == null)
                {
                    return Results.NotFound(new { error = $"Order {id} not found" });
                }
                // Log performance
                await signalRLoggingService.LogPerformanceAsync("GetOrder",
                    stopwatch.ElapsedMilliseconds,
                    "OrderProcessing.Api",
                    new Dictionary<string, object>
                    {
                        { "Success", true },
                        { "OrderId", order.Id }
                    });

                logger.LogInformation("Order {OrderId} retrieved in {Duration}ms",
                    order.Id, stopwatch.ElapsedMilliseconds);

                var orderMapper = endpoints.ServiceProvider.GetRequiredService<OrderMapper>();
                return Results.Ok(new
                {
                    OrderId = order.Id,
                    Status = "Processing",
                    Customer = "test-customer",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    ProcessingTime = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorProperties = new Dictionary<string, string>
                {
                    { "RequestId", requestId },
                    { "OrderId", id.ToString() },
                    { "ExceptionType", ex.GetType().Name },
                    { "ExceptionMessage", ex.Message }
                };

                await signalRLoggingService.LogAsync("Error",
                    $"Failed to retrieve order {id}: {ex.Message}",
                    "OrderProcessing.Api",
                    "Order",
                    errorProperties);

                logger.LogError(ex, "Failed to retrieve order {OrderId}", id);

                return Results.Problem(
                    title: "Order retrieval failed",
                    detail: ex.Message,
                    statusCode: 500);
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

    endpoints.MapPost("/{orderId}/cancel", async (
    string orderId,
    ISignalRLoggingService signalRLoggingService,
    ILogger<Program> logger,
    HttpContext context) =>
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var properties = new Dictionary<string, string>
                {
                    { "RequestId", requestId },
                    { "OrderId", orderId }
                };

                await signalRLoggingService.LogAsync("Information",
                    $"Cancelling order {orderId}",
                    "OrderProcessing.Api",
                    "Order",
                    properties);

                // Simulate potential failure for demonstration (20% chance)
                if (Random.Shared.Next(1, 6) == 1)
                {
                    await signalRLoggingService.LogAsync("Error",
                        $"Failed to cancel order {orderId} - external payment service unavailable",
                        "OrderProcessing.Api",
                        "Order",
                        properties);

                    throw new InvalidOperationException("External payment service is unavailable");
                }

                // Simulate processing time
                await Task.Delay(Random.Shared.Next(100, 300));

                stopwatch.Stop();

                // Log performance
                await signalRLoggingService.LogPerformanceAsync("CancelOrder",
                    stopwatch.ElapsedMilliseconds,
                    "OrderProcessing.Api",
                    new Dictionary<string, object>
                    {
                { "Success", true },
                { "OrderId", orderId }
                    });

                await signalRLoggingService.LogAsync("Information",
                    $"Order {orderId} cancelled successfully",
                    "OrderProcessing.Api",
                    "Order",
                    properties);

                logger.LogInformation("Order {OrderId} cancelled in {Duration}ms",
                    orderId, stopwatch.ElapsedMilliseconds);

                return Results.Ok(new
                {
                    OrderId = orderId,
                    Status = "Cancelled",
                    CancelledAt = DateTime.UtcNow,
                    ProcessingTime = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorProperties = new Dictionary<string, string>
                {
                    { "RequestId", requestId },
                    { "OrderId", orderId },
                    { "ExceptionType", ex.GetType().Name },
                    { "ExceptionMessage", ex.Message },
                    { "StackTrace", ex.StackTrace ?? "" }
                };

                await signalRLoggingService.LogAsync("Error",
                    $"Order cancellation failed for {orderId}: {ex.Message}",
                    "OrderProcessing.Api",
                    "Order",
                    errorProperties);

                // Log performance for failed operation
                await signalRLoggingService.LogPerformanceAsync("CancelOrder",
                    stopwatch.ElapsedMilliseconds,
                    "OrderProcessing.Api",
                    new Dictionary<string, object>
                    {
                { "Success", false },
                { "OrderId", orderId },
                { "ExceptionType", ex.GetType().Name }
                    });

                logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);

                return Results.Problem(
                    title: "Order cancellation failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("CancelOrder")
        .WithSummary("Cancel an order")
        .WithDescription("Cancels the specified order and processes refund")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status500InternalServerError);


        //to be moved
        // Health check endpoint
        endpoints.MapGet("/health", async (ISignalRLoggingService signalRLoggingService) =>
        {
            await signalRLoggingService.LogAsync("Information",
                "Health check requested",
                "OrderProcessing.Api",
                "Health",
                new Dictionary<string, string> { { "Timestamp", DateTime.UtcNow.ToString() } });

            return Results.Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "OrderProcessing.Api"
            });
        })
        .WithTags("Health")
        .WithName("HealthCheck")
        .WithSummary("API health check")
        .Produces<object>(StatusCodes.Status200OK);

        // Feature toggle status endpoint
        endpoints.MapGet("/api/features", async (ISignalRLoggingService signalRLoggingService) =>
        {
            try
            {
                var realTimeLogging = await signalRLoggingService.IsFeatureEnabledAsync("RealTimeLogging");
                var detailedErrorLogging = await signalRLoggingService.IsFeatureEnabledAsync("DetailedErrorLogging");
                var performanceLogging = await signalRLoggingService.IsFeatureEnabledAsync("PerformanceLogging");

                return Results.Ok(new
                {
                    RealTimeLogging = realTimeLogging,
                    DetailedErrorLogging = detailedErrorLogging,
                    PerformanceLogging = performanceLogging,
                    CheckedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    RealTimeLogging = false,
                    DetailedErrorLogging = false,
                    PerformanceLogging = false,
                    Error = ex.Message,
                    CheckedAt = DateTime.UtcNow
                });
            }
        })
        .WithTags("Features")
        .WithName("GetFeatureStatus")
        .WithSummary("Get feature toggle status")
        .Produces<object>(StatusCodes.Status200OK);

        return endpoints;
    }

}
