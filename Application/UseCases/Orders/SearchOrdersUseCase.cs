using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Application.UseCases.Orders;

public class SearchOrdersUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISmartSearchService _smartSearchService;

    public SearchOrdersUseCase(
        IOrderRepository orderRepository, 
        ICustomerRepository customerRepository,
        ISmartSearchService smartSearchService)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _smartSearchService = smartSearchService;
    }

    public async Task<IEnumerable<Order>> Execute(string searchTerm)
    {
        var orders = await _orderRepository.GetAllAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return orders;

        var customers = (await _customerRepository.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);

        return _smartSearchService.Search(
            orders, 
            searchTerm, 
            o => 
            {
                var customerName = customers.TryGetValue(o.CustomerId, out var name) ? name : string.Empty;
                return $"{o.Id} {customerName} {o.Status} {o.Source}".Trim();
            }
        );
    }
}
