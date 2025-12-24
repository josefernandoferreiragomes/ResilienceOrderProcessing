using OrderProcessing.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAppServices();
var app = builder.Build();

await app.ConfigureAppPipeline();
await app.RunAsync();
