using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Api.Dtos;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Services.Resilience;

namespace OrderProcessing.Api.Endpoints;

public static class DemoEndpoints
{
    //public static void MapDemoEndpoints(this WebApplication app)
    //{
    //    var demo = app.MapGroup("/api/demo")
    //        .WithOpenApi()
    //        .WithTags("Demo");
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder demo)
    {
        // Create a test order that will trigger various resilience patterns
        demo.MapPost("/test-resilience", async (
            TestResilienceRequest request,
            IOrderService orderService,
            ILogger<Program> logger) =>
        {
            var results = new List<TestResult>();

            // Create multiple orders to test different scenarios
            for (int i = 0; i < request.NumberOfOrders; i++)
            {
                try
                {
                    var orderRequest = new CreateOrderRequest
                    {
                        CustomerId = $"test-customer-{i}",
                        Items = new List<CreateOrderItemRequest>
                        {
                            new() { ProductId = "LAPTOP-001", ProductName = "Test Laptop", Quantity = 1, UnitPrice = 999.99m },
                            new() { ProductId = "MOUSE-001", ProductName = "Test Mouse", Quantity = 2, UnitPrice = 29.99m }
                        },
                        PaymentMethod = new PaymentMethodRequest { Method = "CreditCard", Token = "test-token" },
                        DeliveryAddress = new AddressRequest
                        {
                            Street = "123 Test St",
                            City = "Test City",
                            State = "TS",
                            ZipCode = "12345",
                            Country = "USA"
                        }
                    };

                    var startTime = DateTime.UtcNow;
                    var order = await orderService.CreateOrderAsync(orderRequest);

                    // Process the order to trigger external service calls
                    var processedOrder = await orderService.ProcessOrderAsync(order.Id);
                    var endTime = DateTime.UtcNow;

                    results.Add(new TestResult
                    {
                        OrderId = processedOrder.Id,
                        Status = processedOrder.Status.ToString(),
                        ProcessingTimeMs = (endTime - startTime).TotalMilliseconds,
                        Success = processedOrder.Status != Core.Models.OrderStatus.Failed,
                        FailureReason = processedOrder.FailureReason
                    });

                    logger.LogInformation("Test order {OrderNumber}/{Total} completed: {Status}",
                        i + 1, request.NumberOfOrders, processedOrder.Status);
                }
                catch (Exception ex)
                {
                    results.Add(new TestResult
                    {
                        OrderId = Guid.Empty,
                        Status = "Exception",
                        ProcessingTimeMs = 0,
                        Success = false,
                        FailureReason = ex.Message
                    });

                    logger.LogError(ex, "Test order {OrderNumber}/{Total} failed with exception",
                        i + 1, request.NumberOfOrders);
                }

                // Add delay between requests if specified
                if (request.DelayBetweenRequestsMs > 0)
                {
                    await Task.Delay(request.DelayBetweenRequestsMs);
                }
            }

            var summary = new TestSummary
            {
                TotalOrders = request.NumberOfOrders,
                SuccessfulOrders = results.Count(r => r.Success),
                FailedOrders = results.Count(r => !r.Success),
                AverageProcessingTimeMs = results.Where(r => r.ProcessingTimeMs > 0).Average(r => r.ProcessingTimeMs),
                Results = results
            };

            return Results.Ok(summary);
        })
        .WithName("TestResilience")
        .WithSummary("Create multiple test orders to demonstrate resilience patterns")
        .Produces<TestSummary>(200);

        // Simulate high load to trigger circuit breakers
        demo.MapPost("/simulate-load", async (
            SimulateLoadRequest request,
            IOrderService orderService,
            ILogger<Program> logger) =>
        {
            var tasks = new List<Task<LoadTestResult>>();

            // Create concurrent tasks
            for (int i = 0; i < request.ConcurrentRequests; i++)
            {
                tasks.Add(SimulateOrderProcessing(i, orderService, logger));
            }

            var results = await Task.WhenAll(tasks);

            var loadTestSummary = new LoadTestSummary
            {
                ConcurrentRequests = request.ConcurrentRequests,
                TotalRequests = results.Length,
                SuccessfulRequests = results.Count(r => r.Success),
                FailedRequests = results.Count(r => !r.Success),
                AverageResponseTimeMs = results.Average(r => r.ResponseTimeMs),
                MaxResponseTimeMs = results.Max(r => r.ResponseTimeMs),
                MinResponseTimeMs = results.Min(r => r.ResponseTimeMs),
                RequestsPerSecond = results.Length / results.Max(r => r.ResponseTimeMs) * 1000,
                Results = results.ToList()
            };

            return Results.Ok(loadTestSummary);
        })
        .WithName("SimulateLoad")
        .WithSummary("Simulate high load to test circuit breaker behavior")
        .Produces<LoadTestSummary>(200);

        // Reset circuit breakers (for demo purposes)
        demo.MapPost("/reset-circuit-breakers", (ICircuitBreakerMonitor monitor, ILogger<Program> logger) =>
        {
            // In a real scenario, you wouldn't typically reset circuit breakers manually
            // This is just for demonstration purposes
            logger.LogWarning("Circuit breaker reset requested - this is for demo purposes only");

            return Results.Ok(new { message = "Circuit breakers reset (demo only)", timestamp = DateTime.UtcNow });
        })
        .WithName("ResetCircuitBreakers")
        .WithSummary("Reset circuit breakers (demo purposes only)")
        .Produces(200);

        return demo;
    }

    

    private static async Task<LoadTestResult> SimulateOrderProcessing(int requestId, IOrderService orderService, ILogger<Program> logger)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var orderRequest = new CreateOrderRequest
            {
                CustomerId = $"load-test-customer-{requestId}",
                Items = new List<CreateOrderItemRequest>
                {
                    new() { ProductId = "LAPTOP-001", ProductName = "Load Test Laptop", Quantity = 1, UnitPrice = 1299.99m }
                },
                PaymentMethod = new PaymentMethodRequest { Method = "CreditCard", Token = $"load-test-token-{requestId}" },
                DeliveryAddress = new AddressRequest
                {
                    Street = $"{requestId} Load Test Ave",
                    City = "Load Test City",
                    State = "LT",
                    ZipCode = "54321",
                    Country = "USA"
                }
            };

            var order = await orderService.CreateOrderAsync(orderRequest);
            var processedOrder = await orderService.ProcessOrderAsync(order.Id);
            var endTime = DateTime.UtcNow;

            return new LoadTestResult
            {
                RequestId = requestId,
                Success = processedOrder.Status != Core.Models.OrderStatus.Failed,
                ResponseTimeMs = (endTime - startTime).TotalMilliseconds,
                Status = processedOrder.Status.ToString(),
                ErrorMessage = processedOrder.FailureReason
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            logger.LogError(ex, "Load test request {RequestId} failed", requestId);

            return new LoadTestResult
            {
                RequestId = requestId,
                Success = false,
                ResponseTimeMs = (endTime - startTime).TotalMilliseconds,
                Status = "Exception",
                ErrorMessage = ex.Message
            };
        }
    }
}



//// OrderProcessing.Api/Extensions/DemoExtensions.cs - Usage in Program.cs
//public static class DemoExtensions
//{
//    public static void MapDemoEndpoints(this WebApplication app)
//    {
//        DemoEndpoints.MapDemoEndpoints(app);
//    }
//}