using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<IEnumerable<Supplier>> GetActiveAsync();
    Task SaveAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Guid id);
}
