using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrderProcessing.Api.Tests.Integration;

public class OrderEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateOrder_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            CustomerId = "CUST123",
            // Add other required properties for CreateOrderRequest
        };

        // Act
        var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        // Optionally, deserialize and assert response content
    }

    [Fact]
    public async Task Get_OrderById_ReturnsNotFound_ForUnknownId()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/{unknownId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}