using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<IEnumerable<Purchase>> GetBySupplierIdAsync(Guid supplierId);
    Task<IEnumerable<Purchase>> GetAllAsync();
    Task SaveAsync(Purchase purchase);
    Task UpdateAsync(Purchase purchase);
    Task DeleteAsync(Guid id);
}
