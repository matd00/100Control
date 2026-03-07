using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfPurchaseRepository : IPurchaseRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfPurchaseRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Purchase?> GetByIdAsync(Guid id)
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Purchase>> GetAllAsync()
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .ToListAsync();
    }

    public async Task<IEnumerable<Purchase>> GetBySupplierIdAsync(Guid supplierId)
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .Where(p => p.SupplierId == supplierId)
            .ToListAsync();
    }

    public async Task SaveAsync(Purchase purchase)
    {
        var existing = await _context.Purchases.FindAsync(purchase.Id);
        if (existing == null)
        {
            _context.Purchases.Add(purchase);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(purchase);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Purchase purchase)
    {
        _context.Purchases.Update(purchase);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var purchase = await _context.Purchases.FindAsync(id);
        if (purchase != null)
        {
            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();
        }
    }
}
