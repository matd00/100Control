using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IInventoryMovementRepository
{
    Task<IEnumerable<InventoryMovement>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryMovement>> GetAllAsync();
    Task SaveAsync(InventoryMovement movement);
}
