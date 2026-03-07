using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfShipmentRepository : IShipmentRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfShipmentRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Shipment?> GetByIdAsync(Guid id)
    {
        return await _context.Shipments
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Shipment?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Shipments
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.OrderId == orderId);
    }

    public async Task<IEnumerable<Shipment>> GetAllAsync()
    {
        return await _context.Shipments
            .Include(s => s.Items)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shipment>> GetByStatusAsync(ShipmentStatus status)
    {
        return await _context.Shipments
            .Include(s => s.Items)
            .Where(s => s.Status == status)
            .ToListAsync();
    }

    public async Task SaveAsync(Shipment shipment)
    {
        var existing = await _context.Shipments.FindAsync(shipment.Id);
        if (existing == null)
        {
            _context.Shipments.Add(shipment);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(shipment);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Shipment shipment)
    {
        _context.Shipments.Update(shipment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var shipment = await _context.Shipments.FindAsync(id);
        if (shipment != null)
        {
            _context.Shipments.Remove(shipment);
            await _context.SaveChangesAsync();
        }
    }
}
