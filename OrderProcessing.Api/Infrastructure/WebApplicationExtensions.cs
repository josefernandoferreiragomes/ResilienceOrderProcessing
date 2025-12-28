
using OrderProcessing.Api.Endpoints;
using OrderProcessing.Core.Models;
using OrderProcessing.Infrastructure.Data;

namespace OrderProcessing.Api.Infrastructure;

public static class WebApplicationExtensions
{
    public static Task ConfigureAppPipeline(this WebApplication app)
    {

        // Map demo endpoints
        var demo = app.MapGroup("/api/demo")
            .WithOpenApi()
            .WithTags("Demo");
        demo.MapDemoEndpoints();

        // Map resilience monitoring endpoints
        var resilience = app.MapGroup("/api/resilience")
            .WithOpenApi()
            .WithTags("Resilience");
        resilience.MapResilienceMonitoringEndpoints();

        //Map order endpoints
        var orders = app.MapGroup("/api/orders")
            .WithOpenApi()
            .WithTags("Orders");
        orders.MapOrderEndpoints();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Add health check endpoint
        app.MapHealthChecks("/health");

        // Seed database with sample data
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
            return Task.FromResult(SeedData(context));
        }



    }

    static async Task SeedData(OrderContext context)
    {
        if (!context.Orders.Any())
        {
            // Add some sample data for testing
            var sampleOrder = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = "customer-123",
                Status = OrderStatus.Created,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = 299.97m,
                Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductId = "LAPTOP-001", ProductName = "Gaming Laptop", Quantity = 5, UnitPrice = 199.99m },
                new() { Id = Guid.NewGuid(), ProductId = "MOUSE-001", ProductName = "Gaming Mouse", Quantity = 2, UnitPrice = 49.99m }
            }
            };

            context.Orders.Add(sampleOrder);
            await context.SaveChangesAsync();
        }
    }
}
