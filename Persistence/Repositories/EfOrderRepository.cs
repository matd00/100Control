using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfOrderRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.Status == status)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task SaveAsync(Order order)
    {
        var existing = await _context.Orders.FindAsync(order.Id);
        if (existing == null)
        {
            _context.Orders.Add(order);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(order);
        }
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
        }
    }
}
