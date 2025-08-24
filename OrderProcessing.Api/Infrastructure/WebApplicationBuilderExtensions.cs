using Microsoft.EntityFrameworkCore;
using OrderProcessing.Api.Mappers;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Repositories;
using OrderProcessing.Services;
using OrderProcessing.Services.Configuration;
using OrderProcessing.Services.External;
using OrderProcessing.Services.Resilience;
using Serilog;

namespace OrderProcessing.Api.Infrastructure;
public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureAppServices(this WebApplicationBuilder builder)
    {
        //Aspire
        builder.AddServiceDefaults();

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
        builder.Services.AddScoped<HttpInventoryService>();
        builder.Services.AddScoped<MockPaymentService>();
        builder.Services.AddScoped<MockShippingService>();

        // Add resilient decorators for external services
        //missing adaptation to HttpInventoryService instead of MockInventoryService
        //builder.Services.AddScoped<IInventoryService, HttpInventoryService>();

        builder.Services.AddScoped<IInventoryService>(provider =>
        {
            var mockService = provider.GetRequiredService<HttpInventoryService>();
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
        builder.Services.AddHttpClient("InventoryService", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7261"); // Update port as needed
        });

        // Add health checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<OrderContext>()
            .AddCheck<ResilienceHealthCheck>("resilience");

        return builder;
    }

}
