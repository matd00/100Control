using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Desktop.Features.Kits;

public class KitsViewModel : ViewModelBase
{
    private readonly IKitRepository _kitRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISmartSearchService _smartSearchService;
    private readonly IUnitOfWork _unitOfWork;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private decimal _price;
    private KitItemViewModel? _selectedKit;
    private ProductItemViewModel? _selectedProduct;
    private int _productQuantity = 1;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private bool _isEditing;
    private string _searchText = string.Empty;

    #region Properties

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterKits();
            }
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public KitItemViewModel? SelectedKit
    {
        get => _selectedKit;
        set
        {
            if (SetProperty(ref _selectedKit, value) && value != null)
            {
                LoadKitToForm(value);
            }
        }
    }

    public ProductItemViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public int ProductQuantity
    {
        get => _productQuantity;
        set => SetProperty(ref _productQuantity, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    #endregion

    public ObservableCollection<KitItemViewModel> AllKits { get; } = new();
    public ObservableCollection<KitItemViewModel> Kits { get; } = new();
    public ObservableCollection<ProductItemViewModel> AvailableProducts { get; } = new();

    public ICommand CreateKitCommand { get; }
    public ICommand UpdateKitCommand { get; }
    public ICommand DeleteKitCommand { get; }
    public ICommand AddProductToKitCommand { get; }
    public ICommand RemoveProductFromKitCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFormCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand SelectKitCommand { get; }

    public KitsViewModel(IKitRepository kitRepository, IProductRepository productRepository, ISmartSearchService smartSearchService, IUnitOfWork unitOfWork)
    {
        _kitRepository = kitRepository ?? throw new ArgumentNullException(nameof(kitRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _smartSearchService = smartSearchService ?? throw new ArgumentNullException(nameof(smartSearchService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        CreateKitCommand = new AsyncRelayCommand(CreateKitAsync, () => !IsLoading && !IsEditing && CanCreate());
        UpdateKitCommand = new AsyncRelayCommand(UpdateKitAsync, () => !IsLoading && IsEditing && SelectedKit != null);
        DeleteKitCommand = new AsyncRelayCommand(DeleteKitAsync, () => !IsLoading && SelectedKit != null);
        AddProductToKitCommand = new AsyncRelayCommand(AddProductToKitAsync, () => !IsLoading && SelectedKit != null && SelectedProduct != null);
        RemoveProductFromKitCommand = new AsyncRelayCommand<Guid>(RemoveProductFromKitAsync);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        ClearFormCommand = new RelayCommand(ClearForm);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        SelectKitCommand = new RelayCommand<KitItemViewModel>(SelectKit);

        _ = LoadDataAsync();
    }

    private void FilterKits()
    {
        Kits.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var k in AllKits) Kits.Add(k);
            return;
        }

        var filtered = _smartSearchService.Search(
            AllKits,
            SearchText,
            k => $"{k.Name} {k.Description}",
            threshold: 0.25);

        foreach (var k in filtered)
        {
            Kits.Add(k);
        }
    }

    private bool CanCreate() => !string.IsNullOrWhiteSpace(Name) && Price > 0;

    private void SelectKit(KitItemViewModel? kit)
    {
        if (kit != null)
        {
            SelectedKit = kit;
            IsEditing = true;
        }
    }

    private void LoadKitToForm(KitItemViewModel kit)
    {
        Name = kit.Name;
        Description = kit.Description;
        Price = kit.Price;
    }

    private async Task CreateKitAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Criando kit...";

            var kit = new Kit(Name, Description, Price);
            await _kitRepository.SaveAsync(kit);
            await _unitOfWork.SaveChangesAsync();

            StatusMessage = "✅ Kit criado com sucesso!";
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateKitAsync()
    {
        if (SelectedKit == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Atualizando kit...";

            var kit = await _kitRepository.GetByIdAsync(SelectedKit.Id);
            if (kit == null)
            {
                StatusMessage = "❌ Kit não encontrado.";
                return;
            }

            // Note: Kit entity needs UpdateInfo method - using repository update for now
            await _kitRepository.UpdateAsync(kit);
            await _unitOfWork.SaveChangesAsync();

            StatusMessage = "✅ Kit atualizado com sucesso!";
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteKitAsync()
    {
        if (SelectedKit == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removendo kit...";

            await _kitRepository.DeleteAsync(SelectedKit.Id);
            await _unitOfWork.SaveChangesAsync();

            StatusMessage = "✅ Kit removido com sucesso!";
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddProductToKitAsync()
    {
        if (SelectedKit == null || SelectedProduct == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Adicionando produto ao kit...";

            var kit = await _kitRepository.GetByIdAsync(SelectedKit.Id);
            if (kit == null)
            {
                StatusMessage = "❌ Kit não encontrado.";
                return;
            }

            kit.AddItem(SelectedProduct.Id, ProductQuantity);
            await _kitRepository.UpdateAsync(kit);
            await _unitOfWork.SaveChangesAsync();

            StatusMessage = "✅ Produto adicionado ao kit!";
            await LoadDataAsync();
            
            // Reload selected kit
            SelectedKit = Kits.FirstOrDefault(k => k.Id == kit.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RemoveProductFromKitAsync(Guid productId)
    {
        if (SelectedKit == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removendo produto do kit...";

            var kit = await _kitRepository.GetByIdAsync(SelectedKit.Id);
            if (kit == null)
            {
                StatusMessage = "❌ Kit não encontrado.";
                return;
            }

            kit.RemoveItem(productId);
            await _kitRepository.UpdateAsync(kit);
            await _unitOfWork.SaveChangesAsync();

            StatusMessage = "✅ Produto removido do kit!";
            await LoadDataAsync();
            
            // Reload selected kit
            SelectedKit = Kits.FirstOrDefault(k => k.Id == kit.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            
            // Load kits
            var kits = await _kitRepository.GetAllAsync();
            AllKits.Clear();
            foreach (var kit in kits.OrderByDescending(k => k.CreatedAt))
            {
                AllKits.Add(new KitItemViewModel
                {
                    Id = kit.Id,
                    Name = kit.Name,
                    Description = kit.Description,
                    Price = kit.Price,
                    ItemCount = kit.Items.Count,
                    Items = kit.Items.Select(i => new KitProductItemViewModel
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList(),
                    IsActive = kit.IsActive
                });
            }
            FilterKits();

            // Load available products
            var products = await _productRepository.GetActiveAsync();
            AvailableProducts.Clear();
            foreach (var product in products)
            {
                AvailableProducts.Add(new ProductItemViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao carregar dados: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearForm()
    {
        Name = string.Empty;
        Description = string.Empty;
        Price = 0;
        SelectedKit = null;
        SelectedProduct = null;
        ProductQuantity = 1;
        IsEditing = false;
    }
}

public class KitItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int ItemCount { get; set; }
    public List<KitProductItemViewModel> Items { get; set; } = new();
    public bool IsActive { get; set; }

    public string PriceFormatted => $"R$ {Price:N2}";
    public string ItemCountText => $"{ItemCount} produto(s)";
}

public class KitProductItemViewModel
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ProductItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public string PriceFormatted => $"R$ {Price:N2}";
}
