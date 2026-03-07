using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfPartRepository : IPartRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfPartRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Part?> GetByIdAsync(Guid id)
    {
        return await _context.Parts.FindAsync(id);
    }

    public async Task<IEnumerable<Part>> GetAllAsync()
    {
        return await _context.Parts.ToListAsync();
    }

    public async Task<IEnumerable<Part>> GetByProductIdAsync(Guid productId)
    {
        return await _context.Parts.Where(p => p.ProductId == productId).ToListAsync();
    }

    public async Task<IEnumerable<Part>> GetActiveAsync()
    {
        return await _context.Parts.Where(p => p.IsActive).ToListAsync();
    }

    public async Task SaveAsync(Part part)
    {
        var existing = await _context.Parts.FindAsync(part.Id);
        if (existing == null)
        {
            _context.Parts.Add(part);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(part);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Part part)
    {
        _context.Parts.Update(part);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part != null)
        {
            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
        }
    }
}
