using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> GetActiveAsync();
    Task SaveAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}
