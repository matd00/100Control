using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IKitRepository
{
    Task<Kit?> GetByIdAsync(Guid id);
    Task<IEnumerable<Kit>> GetAllAsync();
    Task<IEnumerable<Kit>> GetActiveAsync();
    Task SaveAsync(Kit kit);
    Task UpdateAsync(Kit kit);
    Task DeleteAsync(Guid id);
}
