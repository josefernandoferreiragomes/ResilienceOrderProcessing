using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using OrderProcessing.Infrastructure.Data;

namespace OrderProcessing.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderContext _context;

    public OrderRepository(OrderContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateAsync(Order order)
    {
        order.Id = Guid.NewGuid();
        order.CreatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            item.Id = Guid.NewGuid();
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersNextPageAsync(int page, int pageCount)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(page * pageCount)
            .ToListAsync();
    }
}