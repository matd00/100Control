using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Application.UseCases.Customers;

public class SearchCustomersUseCase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISmartSearchService _smartSearchService;

    public SearchCustomersUseCase(ICustomerRepository customerRepository, ISmartSearchService smartSearchService)
    {
        _customerRepository = customerRepository;
        _smartSearchService = smartSearchService;
    }

    public async Task<IEnumerable<Customer>> Execute(string searchTerm)
    {
        var customers = await _customerRepository.GetAllAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return customers;

        return _smartSearchService.Search(
            customers, 
            searchTerm, 
            c => $"{c.Name} {c.Email} {c.Phone} {c.Document} {c.City}".Trim()
        );
    }
}
