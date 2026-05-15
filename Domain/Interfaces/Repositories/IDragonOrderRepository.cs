using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public interface IDragonOrderRepository
{
    Task<PagedResult<DragonOrder>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, DragonOrderStatus? status = null);
    Task<List<DragonOrder>> GetAllAsync();
    Task<DragonOrder?> GetByIdAsync(Guid id);
    Task<List<DragonOrder>> GetByStatusAsync(DragonOrderStatus status);
    Task<List<DragonOrder>> GetPendingPaymentsAsync();
    Task<List<DragonOrder>> GetFactoryUnpaidAsync();
    Task AddAsync(DragonOrder order);
    Task UpdateAsync(DragonOrder order);
    Task DeleteAsync(Guid id);
}
