using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Application.UseCases.Kits;

public class SearchKitsUseCase
{
    private readonly IKitRepository _kitRepository;
    private readonly ISmartSearchService _smartSearchService;

    public SearchKitsUseCase(IKitRepository kitRepository, ISmartSearchService smartSearchService)
    {
        _kitRepository = kitRepository;
        _smartSearchService = smartSearchService;
    }

    public async Task<IEnumerable<Kit>> Execute(string searchTerm)
    {
        var kits = await _kitRepository.GetAllAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return kits;

        return _smartSearchService.Search(
            kits, 
            searchTerm, 
            k => $"{k.Name} {k.Description}".Trim()
        );
    }
}
