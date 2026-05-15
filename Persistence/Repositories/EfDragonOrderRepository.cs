using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfDragonOrderRepository : IDragonOrderRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfDragonOrderRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DragonOrder>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, DragonOrderStatus? status = null)
    {
        var query = _context.DragonOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(o => 
                o.CustomerName.ToLower().Contains(search) || 
                (o.Notes != null && o.Notes.ToLower().Contains(search)));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DragonOrder>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };
    }

    public async Task<List<DragonOrder>> GetAllAsync()
    {
        return await _context.DragonOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<DragonOrder?> GetByIdAsync(Guid id)
    {
        return await _context.DragonOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<DragonOrder>> GetByStatusAsync(DragonOrderStatus status)
    {
        return await _context.DragonOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DragonOrder>> GetPendingPaymentsAsync()
    {
        return await _context.DragonOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Where(o => !o.IsFullyPaid && o.Status != DragonOrderStatus.Cancelado && !o.IsOwnOrder)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DragonOrder>> GetFactoryUnpaidAsync()
    {
        return await _context.DragonOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Where(o => !o.IsFactoryPaid && o.Status != DragonOrderStatus.Cancelado)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(DragonOrder order)
    {
        await _context.DragonOrders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DragonOrder order)
    {
        var existing = await _context.DragonOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        if (existing == null)
            throw new InvalidOperationException("Pedido não encontrado no banco de dados.");

        // Update root properties
        _context.Entry(existing).CurrentValues.SetValues(order);

        // Update Items collection
        foreach (var item in order.Items)
        {
            var existingItem = existing.Items.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem == null)
            {
                existing.Items.Add(item);
            }
            else
            {
                _context.Entry(existingItem).CurrentValues.SetValues(item);
            }
        }
        existing.Items.RemoveAll(i => !order.Items.Any(ni => ni.Id == i.Id));

        // Update Payments collection
        foreach (var p in order.Payments)
        {
            var existingPayment = existing.Payments.FirstOrDefault(ep => ep.Id == p.Id);
            if (existingPayment == null)
            {
                existing.Payments.Add(p);
            }
            else
            {
                _context.Entry(existingPayment).CurrentValues.SetValues(p);
            }
        }
        existing.Payments.RemoveAll(p => !order.Payments.Any(np => np.Id == p.Id));

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.DragonOrders.FindAsync(id);
        if (order != null)
        {
            _context.DragonOrders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }
}
