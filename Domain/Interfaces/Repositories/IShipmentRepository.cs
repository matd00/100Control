using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Shipment>> GetAllAsync();
    Task<IEnumerable<Shipment>> GetByStatusAsync(ShipmentStatus status);
    Task SaveAsync(Shipment shipment);
    Task UpdateAsync(Shipment shipment);
    Task DeleteAsync(Guid id);
}
