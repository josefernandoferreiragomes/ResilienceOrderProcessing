using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace OrderProcessing.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Optionally override ConfigureWebHost to mock services, seed data, etc.
}