using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfSupplierRepository : ISupplierRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfSupplierRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id)
    {
        return await _context.Suppliers.FindAsync(id);
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers.ToListAsync();
    }

    public async Task<IEnumerable<Supplier>> GetActiveAsync()
    {
        return await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
    }

    public async Task SaveAsync(Supplier supplier)
    {
        var existing = await _context.Suppliers.FindAsync(supplier.Id);
        if (existing == null)
        {
            _context.Suppliers.Add(supplier);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(supplier);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
        }
    }
}
