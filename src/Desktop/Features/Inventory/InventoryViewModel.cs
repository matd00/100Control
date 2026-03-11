using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Desktop.Features.Inventory;

public class InventoryMovementItemViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TypeDisplay { get; set; } = string.Empty;
    public InventoryMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string QuantityDisplay => Quantity > 0 ? $"+{Quantity}" : Quantity.ToString();
    public string QuantityColor => Quantity > 0 ? "#22C55E" : "#EF4444";
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public class InventoryViewModel : ViewModelBase
{
    private readonly IInventoryMovementRepository _repository;
    private readonly IProductRepository _productRepository;
    
    private string _searchText = string.Empty;
    private InventoryMovementType? _typeFilter;
    private string _statusMessage = string.Empty;

    public ObservableCollection<InventoryMovementItemViewModel> AllMovements { get; } = new();
    public ObservableCollection<InventoryMovementItemViewModel> Movements { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public InventoryMovementType? TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetProperty(ref _typeFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ClearFilterCommand { get; }

    public InventoryViewModel(
        IInventoryMovementRepository repository,
        IProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;

        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        ClearFilterCommand = new RelayCommand(() => TypeFilter = null);

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        StatusMessage = "Carregando histórico...";
        try
        {
            var movements = await _repository.GetAllAsync();
            var products = await _productRepository.GetAllAsync();
            var productDict = products.ToDictionary(p => p.Id, p => p.Name);

            AllMovements.Clear();
            foreach (var m in movements.OrderByDescending(x => x.CreatedAt))
            {
                AllMovements.Add(new InventoryMovementItemViewModel
                {
                    Id = m.Id,
                    ProductId = m.ProductId,
                    ProductName = productDict.TryGetValue(m.ProductId, out var name) ? name : "Desconhecido",
                    Type = m.Type,
                    TypeDisplay = GetTypeDisplay(m.Type),
                    Quantity = m.Quantity,
                    Reference = m.Reference,
                    Notes = m.Notes ?? string.Empty,
                    CreatedAt = m.CreatedAt
                });
            }

            ApplyFilters();
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private void ApplyFilters()
    {
        Movements.Clear();
        var search = SearchText?.ToLowerInvariant() ?? "";

        foreach (var m in AllMovements)
        {
            if (TypeFilter.HasValue && m.Type != TypeFilter.Value)
                continue;

            if (!string.IsNullOrEmpty(search) && 
                !m.ProductName.ToLowerInvariant().Contains(search) &&
                !m.Reference.ToLowerInvariant().Contains(search))
            {
                continue;
            }

            Movements.Add(m);
        }
    }

    private static string GetTypeDisplay(InventoryMovementType type) => type switch
    {
        InventoryMovementType.Purchase => "Compra",
        InventoryMovementType.Sale => "Venda",
        InventoryMovementType.Adjustment => "Ajuste",
        InventoryMovementType.KitUsage => "Uso em Kit",
        InventoryMovementType.Return => "Devolução",
        _ => type.ToString()
    };
}
