// OrderProcessing.Services/External/MockShippingService.cs
using Microsoft.Extensions.Logging;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;

public class MockShippingService : IShippingService
{
    private readonly ILogger<MockShippingService> _logger;
    private readonly Random _random = new();
    private readonly Dictionary<string, ShippingResult> _shipments = new();

    public MockShippingService(ILogger<MockShippingService> logger)
    {
        _logger = logger;
    }

    public async Task<ShippingQuote> GetShippingQuoteAsync(ShippingQuoteRequest request)
    {
        _logger.LogInformation("Getting shipping quote for delivery to {City}, {State}",
            request.ToAddress.City, request.ToAddress.State);

        // Simulate network delay
        await Task.Delay(_random.Next(300, 800));

        // Simulate occasional failures (5% chance)
        if (_random.Next(1, 101) <= 5)
        {
            _logger.LogWarning("Shipping quote service temporarily unavailable");
            throw new InvalidOperationException("Shipping service temporarily unavailable");
        }

        var baseCost = request.ServiceLevel.ToLower() switch
        {
            "express" => 25.99m,
            "priority" => 15.99m,
            "standard" => 9.99m,
            _ => 9.99m
        };

        var weightMultiplier = Math.Max(1m, request.Weight * 0.5m);
        var finalCost = baseCost * weightMultiplier;

        var deliveryDays = request.ServiceLevel.ToLower() switch
        {
            "express" => 1,
            "priority" => 3,
            "standard" => 7,
            _ => 7
        };

        return new ShippingQuote
        {
            Cost = Math.Round(finalCost, 2),
            Carrier = GetRandomCarrier(),
            ServiceLevel = request.ServiceLevel,
            EstimatedDelivery = DateTime.UtcNow.AddDays(deliveryDays)
        };
    }

    public async Task<ShippingResult> CreateShipmentAsync(ShippingCreateRequest request)
    {
        _logger.LogInformation("Creating shipment for order {OrderId}", request.OrderId);

        // Simulate network delay
        await Task.Delay(_random.Next(500, 1500));

        var trackingNumber = GenerateTrackingNumber();

        // Simulate different scenarios
        var scenario = _random.Next(1, 101);
        ShippingResult result;

        if (scenario <= 10) // 10% chance of failure
        {
            result = new ShippingResult
            {
                IsSuccess = false,
                TrackingNumber = string.Empty,
                Carrier = string.Empty,
                FailureReason = GetRandomShippingFailureReason()
            };
            _logger.LogWarning("Shipment creation failed for order {OrderId}: {Reason}",
                request.OrderId, result.FailureReason);
        }
        else if (scenario <= 15) // 5% chance of timeout/error
        {
            _logger.LogError("Shipping service timeout for order {OrderId}", request.OrderId);
            throw new TimeoutException("Shipping service timeout");
        }
        else // 85% chance of success
        {
            var carrier = GetRandomCarrier();
            var deliveryDays = request.ServiceLevel.ToLower() switch
            {
                "express" => 1,
                "priority" => 3,
                "standard" => 7,
                _ => 7
            };

            result = new ShippingResult
            {
                IsSuccess = true,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                EstimatedDelivery = DateTime.UtcNow.AddDays(deliveryDays)
            };
            _logger.LogInformation("Shipment created successfully for order {OrderId}, tracking: {TrackingNumber}",
                request.OrderId, trackingNumber);
        }

        if (result.IsSuccess)
        {
            _shipments[trackingNumber] = result;
        }

        return result;
    }

    public async Task<ShippingStatus> TrackShipmentAsync(string trackingNumber)
    {
        _logger.LogInformation("Tracking shipment {TrackingNumber}", trackingNumber);

        // Simulate network delay
        await Task.Delay(_random.Next(200, 600));

        if (!_shipments.ContainsKey(trackingNumber))
        {
            throw new ArgumentException($"Tracking number {trackingNumber} not found");
        }

        // Simulate shipment progression
        var statuses = new[] { ShippingStatus.Processing, ShippingStatus.Shipped, ShippingStatus.InTransit, ShippingStatus.Delivered };
        return statuses[_random.Next(statuses.Length)];
    }

    private string GetRandomCarrier()
    {
        var carriers = new[] { "FedEx", "UPS", "DHL", "USPS" };
        return carriers[_random.Next(carriers.Length)];
    }

    private string GenerateTrackingNumber()
    {
        var prefix = GetRandomCarrier().ToUpper().Substring(0, 2);
        var number = _random.Next(100000000, 999999999);
        return $"{prefix}{number}";
    }

    private string GetRandomShippingFailureReason()
    {
        var reasons = new[]
        {
            "Invalid shipping address",
            "Shipping carrier unavailable",
            "Package too heavy for selected service",
            "Restricted delivery area",
            "Shipping service temporarily down"
        };

        return reasons[_random.Next(reasons.Length)];
    }
}