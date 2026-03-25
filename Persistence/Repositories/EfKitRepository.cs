using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfKitRepository : IKitRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfKitRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Kit?> GetByIdAsync(Guid id)
    {
        return await _context.Kits
            .Include(k => k.Items)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<IEnumerable<Kit>> GetAllAsync()
    {
        return await _context.Kits
            .Include(k => k.Items)
            .ToListAsync();
    }

    public async Task<IEnumerable<Kit>> GetActiveAsync()
    {
        return await _context.Kits
            .Include(k => k.Items)
            .Where(k => k.IsActive)
            .ToListAsync();
    }

    public async Task SaveAsync(Kit kit)
    {
        var existing = await _context.Kits.FindAsync(kit.Id);
        if (existing == null)
        {
            _context.Kits.Add(kit);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(kit);
        }
    }

    public async Task UpdateAsync(Kit kit)
    {
        _context.Kits.Update(kit);
    }

    public async Task DeleteAsync(Guid id)
    {
        var kit = await _context.Kits.FindAsync(id);
        if (kit != null)
        {
            _context.Kits.Remove(kit);
        }
    }
}
