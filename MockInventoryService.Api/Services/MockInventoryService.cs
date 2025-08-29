using Microsoft.Extensions.Logging;

namespace MockInventory.Api.Services;

public class MockInventoryService
{
    private readonly ILogger<MockInventoryService> _logger;
    private readonly Random _random = new();

    // Simulated inventory data
    private readonly Dictionary<string, int> _inventory = new()
    {
        { "LAPTOP-001", 10 },
        { "MOUSE-001", 50 },
        { "KEYBOARD-001", 25 },
        { "MONITOR-001", 5 },
        { "HEADSET-001", 15 }
    };

    public MockInventoryService(ILogger<MockInventoryService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CheckAvailabilityAsync(string productId, int quantity)
    {
        _logger.LogInformation("Checking inventory for product {ProductId}, quantity {Quantity}", productId, quantity);

        // Simulate network delay
        await Task.Delay(_random.Next(100, 500));

        // Simulate occasional failures (30% chance)
        if (_random.Next(1, 101) <= 30)
        {
            _logger.LogWarning("Inventory service temporarily unavailable for product {ProductId}", productId);
            throw new InvalidOperationException("Inventory service temporarily unavailable");
        }

        var available = _inventory.GetValueOrDefault(productId, 0) >= quantity;
        _logger.LogInformation("Product {ProductId} availability check: {Available}", productId, available);

        return available;
    }

    public async Task<bool> ReserveInventoryAsync(string productId, int quantity)
    {
        _logger.LogInformation("Reserving inventory for product {ProductId}, quantity {Quantity}", productId, quantity);

        // Simulate network delay
        await Task.Delay(_random.Next(200, 800));

        // Simulate occasional failures (5% chance)
        if (_random.Next(1, 101) <= 5)
        {
            _logger.LogWarning("Failed to reserve inventory for product {ProductId}", productId);
            throw new InvalidOperationException("Failed to reserve inventory");
        }

        if (_inventory.ContainsKey(productId) && _inventory[productId] >= quantity)
        {
            _inventory[productId] -= quantity;
            _logger.LogInformation("Successfully reserved {Quantity} units of product {ProductId}", quantity, productId);
            return true;
        }

        _logger.LogWarning("Insufficient inventory for product {ProductId}", productId);
        return false;
    }

    public async Task<bool> ReleaseInventoryAsync(string productId, int quantity)
    {
        _logger.LogInformation("Releasing inventory for product {ProductId}, quantity {Quantity}", productId, quantity);

        // Simulate network delay
        await Task.Delay(_random.Next(100, 300));

        if (_inventory.ContainsKey(productId))
        {
            _inventory[productId] += quantity;
        }
        else
        {
            _inventory[productId] = quantity;
        }

        _logger.LogInformation("Successfully released {Quantity} units of product {ProductId}", quantity, productId);
        return true;
    }
}