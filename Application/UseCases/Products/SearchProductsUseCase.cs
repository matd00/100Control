using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Application.UseCases.Products;

public class SearchProductsUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISmartSearchService _smartSearchService;

    public SearchProductsUseCase(IProductRepository productRepository, ISmartSearchService smartSearchService)
    {
        _productRepository = productRepository;
        _smartSearchService = smartSearchService;
    }

    public async Task<IEnumerable<Product>> Execute(string searchTerm)
    {
        var products = await _productRepository.GetAllAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return products;

        return _smartSearchService.Search(
            products, 
            searchTerm, 
            p => $"{p.Name} {p.Description} {p.SKU}".Trim()
        );
    }
}
