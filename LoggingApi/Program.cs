// LoggingApi/Program.cs
using LoggingApi.Hubs;
using LoggingApi.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Feature toggle service
builder.Services.AddSingleton<IFeatureToggleService, FeatureToggleService>();

// CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseStaticFiles();         // <-- Add this line
app.UseRouting();

app.MapControllers();
app.MapHub<LoggingHub>("/loggingHub");

app.Run();