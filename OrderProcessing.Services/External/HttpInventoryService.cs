using OrderProcessing.Core.ExternalServices;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OrderProcessing.Services.External;

public class HttpInventoryService : IInventoryService
{
    private readonly HttpClient _client;

    public HttpInventoryService(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("InventoryService");
    }

    public async Task<bool> CheckAvailabilityAsync(string productId, int quantity)
    {
        var response = await _client.GetAsync($"/inventory/{productId}/availability?quantity={quantity}");
        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException("Inventory service temporarily unavailable");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AvailabilityResponse>();
        return result?.isAvailable ?? false;
    }

    public async Task<bool> ReserveInventoryAsync(string productId, int quantity)
    {
        var response = await _client.PostAsync($"/inventory/{productId}/reserve?quantity={quantity}", null);
        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException("Failed to reserve inventory");

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReleaseInventoryAsync(string productId, int quantity)
    {
        var response = await _client.PostAsync($"/inventory/{productId}/release?quantity={quantity}", null);
        return response.IsSuccessStatusCode;
    }

    private record AvailabilityResponse(bool isAvailable);
}