using Microsoft.Extensions.Logging;
using MockInventory.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/mockinventory-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MockInventoryService>();

var app = builder.Build();

//app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add health check endpoint
//app.MapHealthChecks("/health");

// Map inventory endpoints
app.MapGet("/inventory/{productId}/availability", async (string productId, int quantity, MockInventoryService service) =>
{
    try
    {
        var isAvailable = await service.CheckAvailabilityAsync(productId, quantity);
        return Results.Ok(new { isAvailable });
    }
    catch (InvalidOperationException ex)
    {
        Console.Write(ex.ToString());
        return Results.StatusCode(503);
    }
});

app.MapPost("/inventory/{productId}/reserve", async (string productId, int quantity, MockInventoryService service) =>
{
    try
    {
        var isReserved = await service.ReserveInventoryAsync(productId, quantity);
        return isReserved ? Results.Ok() : Results.BadRequest("Insufficient inventory");
    }
    catch (InvalidOperationException ex)
    {
        Console.Write(ex.ToString());
        return Results.StatusCode(503);
    }
});

app.MapPost("/inventory/{productId}/release", async (string productId, int quantity, MockInventoryService service) =>
{
    var isReleased = await service.ReleaseInventoryAsync(productId, quantity);
    return isReleased ? Results.Ok() : Results.BadRequest();
});

app.Run();