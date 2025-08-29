using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace OrderProcessing.Api.Services;

public interface ISignalRLoggingService
{
    Task LogAsync(string level, string message, string source, string category, Dictionary<string, string>? properties = null);
    Task LogPerformanceAsync(string operation, long duration, string source, Dictionary<string, object>? metadata = null);
    Task<bool> IsFeatureEnabledAsync(string featureName);
}

public class SignalRLoggingService : ISignalRLoggingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SignalRLoggingService> _logger;
    private readonly string _loggingApiUrl;

    public SignalRLoggingService(HttpClient httpClient, ILogger<SignalRLoggingService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _loggingApiUrl = configuration.GetValue<string>("LoggingApi:BaseUrl") ?? "https://localhost:7002";
    }

    public async Task LogAsync(string level, string message, string source, string category, Dictionary<string, string>? properties = null)
    {
        try
        {
            var logMessage = new
            {
                Level = level,
                Message = message,
                Source = source,
                Category = category,
                Properties = properties ?? new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(logMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_loggingApiUrl}/api/logging/log", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send log to SignalR service. Status: {StatusCode}, Reason: {Reason}",
                    response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "HTTP error sending log to SignalR logging service");
        }
        catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
        {
            _logger.LogWarning("Timeout sending log to SignalR logging service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending log to SignalR logging service");
        }
    }

    public async Task LogPerformanceAsync(string operation, long duration, string source, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var performanceLog = new
            {
                Operation = operation,
                Duration = duration,
                Source = source,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(performanceLog, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_loggingApiUrl}/api/logging/performance", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send performance log to SignalR service. Status: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "HTTP error sending performance log to SignalR logging service");
        }
        catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
        {
            _logger.LogWarning("Timeout sending performance log to SignalR logging service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending performance log to SignalR logging service");
        }
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_loggingApiUrl}/api/featuretoggle/{featureName}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty("enabled", out var enabledProperty))
                {
                    return enabledProperty.GetBoolean();
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogDebug(httpEx, "HTTP error checking feature toggle for {FeatureName}", featureName);
        }
        catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
        {
            _logger.LogDebug("Timeout checking feature toggle for {FeatureName}", featureName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking feature toggle for {FeatureName}", featureName);
        }

        // Default to false if feature service is unavailable
        return false;
    }
}