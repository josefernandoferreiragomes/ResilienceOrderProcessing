using Microsoft.Extensions.Logging;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.ExternalServices;
using OrderProcessing.Core.ExternalServices.Models;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;

namespace OrderProcessing.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IShippingService shippingService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _shippingService = shippingService;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = OrderStatus.Created,
            Items = request.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            Shipping = new ShippingInfo
            {
                Status = ShippingStatus.Pending,
                DeliveryAddress = new Address
                {
                    Street = request.DeliveryAddress.Street,
                    City = request.DeliveryAddress.City,
                    State = request.DeliveryAddress.State,
                    ZipCode = request.DeliveryAddress.ZipCode,
                    Country = request.DeliveryAddress.Country
                }
            }
        };

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

        var createdOrder = await _orderRepository.CreateAsync(order);
        _logger.LogInformation("Order {OrderId} created successfully", createdOrder.Id);

        return createdOrder;
    }

    public async Task<Order?> GetOrderAsync(Guid id)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);
        return await _orderRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(string customerId)
    {
        _logger.LogInformation("Retrieving orders for customer {CustomerId}", customerId);
        return await _orderRepository.GetByCustomerIdAsync(customerId);
    }

    public async Task<Order> ProcessOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order {orderId} not found");
        }

        _logger.LogInformation("Starting order processing for order {OrderId}", orderId);

        try
        {
            // Step 1: Check Inventory
            await CheckInventoryAsync(order);

            // Step 2: Process Payment
            await ProcessPaymentAsync(order);

            // Step 3: Arrange Shipping
            await ArrangeShippingAsync(order);

            order.Status = OrderStatus.Shipped;
            _logger.LogInformation("Order {OrderId} processing completed successfully", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order {OrderId} processing failed: {Error}", orderId, ex.Message);
            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;
        }

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<Order> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? failureReason = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order {orderId} not found");
        }

        _logger.LogInformation("Updating order {OrderId} status from {OldStatus} to {NewStatus}",
            orderId, order.Status, status);

        order.Status = status;
        order.FailureReason = failureReason;

        return await _orderRepository.UpdateAsync(order);
    }

    private async Task CheckInventoryAsync(Order order)
    {
        _logger.LogInformation("Checking inventory for order {OrderId}", order.Id);

        foreach (var item in order.Items)
        {
            var isAvailable = await _inventoryService.CheckAvailabilityAsync(item.ProductId, item.Quantity);
            if (!isAvailable)
            {
                throw new InvalidOperationException($"Insufficient inventory for product {item.ProductName}");
            }

            var reserved = await _inventoryService.ReserveInventoryAsync(item.ProductId, item.Quantity);
            if (!reserved)
            {
                throw new InvalidOperationException($"Failed to reserve inventory for product {item.ProductName}");
            }
        }

        order.Status = OrderStatus.InventoryChecked;
        await _orderRepository.UpdateAsync(order);
        _logger.LogInformation("Inventory check completed for order {OrderId}", order.Id);
    }

    private async Task ProcessPaymentAsync(Order order)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", order.Id);

        order.Status = OrderStatus.PaymentProcessing;
        await _orderRepository.UpdateAsync(order);

        var paymentRequest = new PaymentRequest
        {
            Amount = order.TotalAmount,
            Currency = "USD",
            PaymentMethod = "CreditCard", // This would come from the create request
            PaymentToken = Guid.NewGuid().ToString(), // Mock token
            OrderId = order.Id.ToString()
        };

        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

        if (paymentResult.IsSuccess)
        {
            order.Payment = new PaymentInfo
            {
                PaymentId = paymentResult.PaymentId,
                PaymentMethod = paymentRequest.PaymentMethod,
                Status = paymentResult.Status,
                ProcessedAt = paymentResult.ProcessedAt
            };
            order.Status = OrderStatus.PaymentCompleted;
            _logger.LogInformation("Payment completed for order {OrderId}", order.Id);
        }
        else
        {
            order.Payment = new PaymentInfo
            {
                PaymentId = paymentResult.PaymentId,
                PaymentMethod = paymentRequest.PaymentMethod,
                Status = paymentResult.Status,
                FailureReason = paymentResult.FailureReason
            };
            order.Status = OrderStatus.PaymentFailed;
            throw new InvalidOperationException($"Payment failed: {paymentResult.FailureReason}");
        }

        await _orderRepository.UpdateAsync(order);
    }

    private async Task ArrangeShippingAsync(Order order)
    {
        _logger.LogInformation("Arranging shipping for order {OrderId}", order.Id);

        order.Status = OrderStatus.Shipping;
        await _orderRepository.UpdateAsync(order);

        var shippingRequest = new ShippingCreateRequest
        {
            OrderId = order.Id.ToString(),
            FromAddress = new Address
            {
                Street = "123 Warehouse St",
                City = "Distribution City",
                State = "DC",
                ZipCode = "12345",
                Country = "USA"
            },
            ToAddress = order.Shipping?.DeliveryAddress ?? new Address(),
            Weight = order.Items.Sum(i => i.Quantity) * 1.5m, // Mock weight calculation
            ServiceLevel = "Standard",
            Items = order.Items
        };

        var shippingResult = await _shippingService.CreateShipmentAsync(shippingRequest);

        if (shippingResult.IsSuccess)
        {
            if (order.Shipping != null)
            {
                order.Shipping.TrackingNumber = shippingResult.TrackingNumber;
                order.Shipping.Carrier = shippingResult.Carrier;
                order.Shipping.Status = ShippingStatus.Shipped;
                order.Shipping.ShippedAt = DateTime.UtcNow;
                order.Shipping.EstimatedDelivery = shippingResult.EstimatedDelivery;
            }

            _logger.LogInformation("Shipping arranged for order {OrderId}, tracking: {TrackingNumber}",
                order.Id, shippingResult.TrackingNumber);
        }
        else
        {
            throw new InvalidOperationException($"Shipping failed: {shippingResult.FailureReason}");
        }

        await _orderRepository.UpdateAsync(order);
    }

    public Task<IEnumerable<Order>> GetOrdersNextPageAsync(int page, int pageCount)
    {
        _logger.LogInformation("Retrieving next orders to process, page {page} with pageCount {pageCount} orders", page, pageCount);
        return _orderRepository.GetOrdersNextPageAsync(page, pageCount);

    }

}