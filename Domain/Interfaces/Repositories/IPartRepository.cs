using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id);
    Task<IEnumerable<Part>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<Part>> GetAllAsync();
    Task<IEnumerable<Part>> GetActiveAsync();
    Task SaveAsync(Part part);
    Task UpdateAsync(Part part);
    Task DeleteAsync(Guid id);
}
