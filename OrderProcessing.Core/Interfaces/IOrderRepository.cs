using OrderProcessing.Core.Models;

namespace OrderProcessing.Core.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId);
    Task<Order> UpdateAsync(Order order);
    Task<IEnumerable<Order>> GetAllAsync();
    Task<IEnumerable<Order>> GetOrdersNextPageAsync(int page, int pageCount);
}