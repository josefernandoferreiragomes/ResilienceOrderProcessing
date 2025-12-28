using OrderProcessing.Core.Dtos;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Models;

namespace OrderProcessing.Core.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task<Order?> GetOrderAsync(Guid id);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(string customerId);
    Task<CustomTestResult<Order>> ProcessOrderAsync(Guid orderId);
    Task<Order> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? failureReason = null);
    Task<IEnumerable<Order>> GetOrdersNextPageAsync(int page, int pageSize);
}