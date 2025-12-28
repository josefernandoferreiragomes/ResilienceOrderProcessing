// OrderProcessing.Api/Program.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockInventory.Api.Services;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Repositories;
using OrderProcessing.Services;
using OrderProcessing.Services.External;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/orderprocessing-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with In-Memory database for demo
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseInMemoryDatabase("OrderProcessingDemo"));

// Add repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Add external service implementations (these will be mock services for now)
builder.Services.AddScoped<IInventoryService, MockInventoryService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();
builder.Services.AddScoped<IShippingService, MockShippingService>();

// Add HTTP clients for external services (will be used later with Polly)
builder.Services.AddHttpClient();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Seed database with sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
    await SeedData(context);
}

app.Run();

static async Task SeedData(OrderContext context)
{
    if (!context.Orders.Any())
    {
        // Add some sample data for testing
        var sampleOrder = new OrderProcessing.Core.Models.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123",
            Status = OrderProcessing.Core.Models.OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = 299.97m,
            Items = new List<OrderProcessing.Core.Models.OrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductId = "LAPTOP-001", ProductName = "Gaming Laptop", Quantity = 1, UnitPrice = 199.99m },
                new() { Id = Guid.NewGuid(), ProductId = "MOUSE-001", ProductName = "Gaming Mouse", Quantity = 2, UnitPrice = 49.99m }
            }
        };

        context.Orders.Add(sampleOrder);
        await context.SaveChangesAsync();
    }
}
