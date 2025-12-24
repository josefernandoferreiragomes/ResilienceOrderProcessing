using OrderProcessing.Core.Dtos;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.ExternalServices;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OrderProcessing.Services.External;

public class HttpInventoryService : IInventoryService
{
    private readonly HttpClient _client;

    //public HttpInventoryService(IHttpClientFactory httpClientFactory)
    //{
    //    _client = httpClientFactory.CreateClient("InventoryService");
    //}
    public HttpInventoryService(HttpClient httpClient)
    {
        _client = httpClient;
    }

    public async Task<CustomTestResult<AvailabilityResponse>> CheckAvailabilityAsync(string productId, int quantity, Guid orderId)
    {
        CustomTestResult<AvailabilityResponse> result = new CustomTestResult<AvailabilityResponse>();

        var serviceResponse = await _client.GetAsync($"/inventory/{productId}/availability?quantity={quantity}");
        if (serviceResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException("Inventory service temporarily unavailable");

        serviceResponse.EnsureSuccessStatusCode();
        var resultFromJson = await serviceResponse.Content.ReadFromJsonAsync<AvailabilityResponse>();
        result.ObjectReference = resultFromJson ?? new AvailabilityResponse();
        return result;
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

    //public Task<CustomTestResult<AvailabilityResponse>> CheckAvailabilityAsync(string productId, int quantity, Guid orderId)
    //{
    //    throw new NotImplementedException();
    //}
}