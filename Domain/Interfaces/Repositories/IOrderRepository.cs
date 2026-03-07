using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetAllAsync();
    Task SaveAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid id);
}
