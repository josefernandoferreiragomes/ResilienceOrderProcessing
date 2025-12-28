using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderProcessing.Core.Interfaces;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real IOrderService registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IOrderService));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add a mock
            var orderServiceMock = new Mock<IOrderService>();
            // Setup mock as needed
            services.AddSingleton(orderServiceMock.Object);
        });
    }
}