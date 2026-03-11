using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IFactoryOrderRepository
{
    Task<List<FactoryOrder>> GetAllAsync();
    Task<FactoryOrder?> GetByIdAsync(Guid id);
    Task<List<FactoryOrder>> GetByStatusAsync(FactoryOrderStatus status);
    Task AddAsync(FactoryOrder order);
    Task UpdateAsync(FactoryOrder order);
    Task DeleteAsync(Guid id);
}
