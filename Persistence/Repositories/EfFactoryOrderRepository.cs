using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfFactoryOrderRepository : IFactoryOrderRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfFactoryOrderRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<List<FactoryOrder>> GetAllAsync()
    {
        return await _context.FactoryOrders
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task<FactoryOrder?> GetByIdAsync(Guid id)
    {
        return await _context.FactoryOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<FactoryOrder>> GetByStatusAsync(FactoryOrderStatus status)
    {
        return await _context.FactoryOrders
            .Include(o => o.Items)
            .Where(o => o.Status == status)
            .ToListAsync();
    }

    public async Task AddAsync(FactoryOrder order)
    {
        _context.FactoryOrders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FactoryOrder order)
    {
        _context.FactoryOrders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.FactoryOrders.FindAsync(id);
        if (order != null)
        {
            _context.FactoryOrders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }
}
