using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfInventoryMovementRepository : IInventoryMovementRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfInventoryMovementRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InventoryMovement>> GetAllAsync()
    {
        return await _context.InventoryMovements.ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetByProductIdAsync(Guid productId)
    {
        return await _context.InventoryMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.InventoryMovements
            .Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync();
    }
}
