using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Application.UseCases.Suppliers;

public class SearchSuppliersUseCase
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISmartSearchService _smartSearchService;

    public SearchSuppliersUseCase(ISupplierRepository supplierRepository, ISmartSearchService smartSearchService)
    {
        _supplierRepository = supplierRepository;
        _smartSearchService = smartSearchService;
    }

    public async Task<IEnumerable<Supplier>> Execute(string searchTerm)
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return suppliers;

        return _smartSearchService.Search(
            suppliers, 
            searchTerm, 
            s => $"{s.Name} {s.ContactName} {s.Email} {s.Phone} {s.Document} {s.City}".Trim()
        );
    }
}
