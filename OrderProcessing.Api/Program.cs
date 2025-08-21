using Microsoft.EntityFrameworkCore;
using OrderProcessing.Api.Endpoints;
using OrderProcessing.Api.Mappers;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Repositories;
using OrderProcessing.Services;
using OrderProcessing.Services.Configuration;
using OrderProcessing.Services.External;
using OrderProcessing.Services.Resilience;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/orderprocessing-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure options
builder.Services.Configure<ResilienceOptions>(builder.Configuration.GetSection(ResilienceOptions.SectionName));
builder.Services.Configure<ExternalServiceOptions>(builder.Configuration.GetSection(ExternalServiceOptions.SectionName));

// Add Entity Framework with In-Memory database for demo
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseInMemoryDatabase("OrderProcessingDemo"));

// Add repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<OrderMapper>();

//// Add external service implementations (these will be mock services for now)
//builder.Services.AddScoped<IInventoryService, MockInventoryService>();
//builder.Services.AddScoped<IPaymentService, MockPaymentService>();
//builder.Services.AddScoped<IShippingService, MockShippingService>();

// Add resilience services
builder.Services.AddSingleton<IResiliencePipelineFactory, ResiliencePipelineFactory>();
builder.Services.AddSingleton<ICircuitBreakerMonitor, CircuitBreakerMonitor>();

// Add mock external services (base implementations)
builder.Services.AddScoped<MockInventoryService>();
builder.Services.AddScoped<MockPaymentService>();
builder.Services.AddScoped<MockShippingService>();

// Add resilient decorators for external services
builder.Services.AddScoped<IInventoryService>(provider =>
{
    var mockService = provider.GetRequiredService<MockInventoryService>();
    var pipelineFactory = provider.GetRequiredService<IResiliencePipelineFactory>();
    var logger = provider.GetRequiredService<ILogger<ResilientInventoryService>>();
    return new ResilientInventoryService(mockService, pipelineFactory, logger);
});

builder.Services.AddScoped<IPaymentService>(provider =>
{
    var mockService = provider.GetRequiredService<MockPaymentService>();
    var pipelineFactory = provider.GetRequiredService<IResiliencePipelineFactory>();
    var logger = provider.GetRequiredService<ILogger<ResilientPaymentService>>();
    return new ResilientPaymentService(mockService, pipelineFactory, logger);
});

builder.Services.AddScoped<IShippingService>(provider =>
{
    var mockService = provider.GetRequiredService<MockShippingService>();
    var pipelineFactory = provider.GetRequiredService<IResiliencePipelineFactory>();
    var logger = provider.GetRequiredService<ILogger<ResilientShippingService>>();
    return new ResilientShippingService(mockService, pipelineFactory, logger);
});

// Add HTTP clients for external services (will be used later with Polly)
builder.Services.AddHttpClient();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderContext>()
    .AddCheck<ResilienceHealthCheck>("resilience");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add health check endpoint
app.MapHealthChecks("/health");

// Map minimal API endpoints
app.MapOrderEndpoints();
app.MapDemoEndpoints();

// Map resilience monitoring endpoints
app.MapResilienceMonitoringEndpoints();

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
        var sampleOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123",
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = 299.97m,
            Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductId = "LAPTOP-001", ProductName = "Gaming Laptop", Quantity = 1, UnitPrice = 199.99m },
                new() { Id = Guid.NewGuid(), ProductId = "MOUSE-001", ProductName = "Gaming Mouse", Quantity = 2, UnitPrice = 49.99m }
            }
        };

        context.Orders.Add(sampleOrder);
        await context.SaveChangesAsync();
    }
}
