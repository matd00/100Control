using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Purchases;

namespace Desktop.Features.Purchases;

public class PurchasesViewModel : ViewModelBase
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly RegisterPurchaseUseCase _registerPurchaseUseCase;

    private SupplierItemViewModel? _selectedSupplier;
    private PurchaseType _purchaseType = PurchaseType.FactoryPurchase;
    private PurchaseItemViewModel? _selectedPurchase;
    private ProductForPurchaseViewModel? _selectedProduct;
    private int _quantity = 1;
    private decimal _cost;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    #region Properties

    public SupplierItemViewModel? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public PurchaseType PurchaseType
    {
        get => _purchaseType;
        set => SetProperty(ref _purchaseType, value);
    }

    public PurchaseItemViewModel? SelectedPurchase
    {
        get => _selectedPurchase;
        set => SetProperty(ref _selectedPurchase, value);
    }

    public ProductForPurchaseViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public decimal Cost
    {
        get => _cost;
        set => SetProperty(ref _cost, value);
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

    #endregion

    public ObservableCollection<PurchaseItemViewModel> Purchases { get; } = new();
    public ObservableCollection<SupplierItemViewModel> Suppliers { get; } = new();
    public ObservableCollection<ProductForPurchaseViewModel> Products { get; } = new();
    public ObservableCollection<PurchaseType> PurchaseTypes { get; } = new()
    {
        PurchaseType.FactoryPurchase,
        PurchaseType.UsedPurchase
    };

    public ICommand CreatePurchaseCommand { get; }
    public ICommand AddItemToPurchaseCommand { get; }
    public ICommand MarkAsReceivedCommand { get; }
    public ICommand CancelPurchaseCommand { get; }
    public ICommand DeletePurchaseCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectPurchaseCommand { get; }

    public PurchasesViewModel(
        IPurchaseRepository purchaseRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        RegisterPurchaseUseCase registerPurchaseUseCase)
    {
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _registerPurchaseUseCase = registerPurchaseUseCase ?? throw new ArgumentNullException(nameof(registerPurchaseUseCase));

        CreatePurchaseCommand = new AsyncRelayCommand(CreatePurchaseAsync, () => !IsLoading && SelectedSupplier != null);
        AddItemToPurchaseCommand = new AsyncRelayCommand(AddItemToPurchaseAsync, () => !IsLoading && SelectedPurchase != null && SelectedProduct != null);
        MarkAsReceivedCommand = new AsyncRelayCommand(MarkAsReceivedAsync, () => !IsLoading && SelectedPurchase != null);
        CancelPurchaseCommand = new AsyncRelayCommand(CancelPurchaseAsync, () => !IsLoading && SelectedPurchase != null);
        DeletePurchaseCommand = new AsyncRelayCommand(DeletePurchaseAsync, () => !IsLoading && SelectedPurchase != null);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        SelectPurchaseCommand = new RelayCommand<PurchaseItemViewModel>(SelectPurchase);

        _ = LoadDataAsync();
    }

    private void SelectPurchase(PurchaseItemViewModel? purchase)
    {
        if (purchase != null)
        {
            SelectedPurchase = purchase;
        }
    }

    private async Task CreatePurchaseAsync()
    {
        if (SelectedSupplier == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Criando compra...";

            var purchase = new Purchase(SelectedSupplier.Id, PurchaseType);
            await _purchaseRepository.SaveAsync(purchase);

            StatusMessage = "✅ Compra criada com sucesso! Adicione itens.";
            await LoadDataAsync();
            
            // Select the new purchase
            SelectedPurchase = Purchases.FirstOrDefault(p => p.Id == purchase.Id);
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

    private async Task AddItemToPurchaseAsync()
    {
        if (SelectedPurchase == null || SelectedProduct == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Adicionando item...";

            var purchase = await _purchaseRepository.GetByIdAsync(SelectedPurchase.Id);
            if (purchase == null)
            {
                StatusMessage = "❌ Compra não encontrada.";
                return;
            }

            purchase.AddItem(SelectedProduct.Id, Quantity, Cost);
            await _purchaseRepository.UpdateAsync(purchase);

            StatusMessage = "✅ Item adicionado!";
            Quantity = 1;
            Cost = 0;
            await LoadDataAsync();
            
            // Reload selected purchase
            SelectedPurchase = Purchases.FirstOrDefault(p => p.Id == purchase.Id);
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

    private async Task MarkAsReceivedAsync()
    {
        if (SelectedPurchase == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Marcando como recebido...";

            var purchase = await _purchaseRepository.GetByIdAsync(SelectedPurchase.Id);
            if (purchase == null)
            {
                StatusMessage = "❌ Compra não encontrada.";
                return;
            }

            purchase.MarkAsReceived(DateTime.UtcNow);
            await _purchaseRepository.UpdateAsync(purchase);

            StatusMessage = "✅ Compra marcada como recebida!";
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

    private async Task CancelPurchaseAsync()
    {
        if (SelectedPurchase == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Cancelando compra...";

            var purchase = await _purchaseRepository.GetByIdAsync(SelectedPurchase.Id);
            if (purchase == null)
            {
                StatusMessage = "❌ Compra não encontrada.";
                return;
            }

            purchase.Cancel();
            await _purchaseRepository.UpdateAsync(purchase);

            StatusMessage = "✅ Compra cancelada!";
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

    private async Task DeletePurchaseAsync()
    {
        if (SelectedPurchase == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removendo compra...";

            await _purchaseRepository.DeleteAsync(SelectedPurchase.Id);

            StatusMessage = "✅ Compra removida!";
            SelectedPurchase = null;
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

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            // Load purchases
            var purchases = await _purchaseRepository.GetAllAsync();
            Purchases.Clear();
            foreach (var purchase in purchases.OrderByDescending(p => p.CreatedAt))
            {
                Purchases.Add(new PurchaseItemViewModel
                {
                    Id = purchase.Id,
                    SupplierId = purchase.SupplierId,
                    Type = purchase.Type,
                    TotalAmount = purchase.TotalAmount,
                    Status = purchase.Status,
                    ItemCount = purchase.Items.Count,
                    PurchaseDate = purchase.PurchaseDate,
                    DeliveryDate = purchase.DeliveryDate
                });
            }

            // Load suppliers
            var suppliers = await _supplierRepository.GetActiveAsync();
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(new SupplierItemViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name
                });
            }

            // Load products
            var products = await _productRepository.GetActiveAsync();
            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(new ProductForPurchaseViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Cost = product.Cost
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
}

public class PurchaseItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public PurchaseType Type { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseStatus Status { get; set; }
    public int ItemCount { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public string TotalFormatted => $"R$ {TotalAmount:N2}";
    public string TypeText => Type == PurchaseType.FactoryPurchase ? "Fábrica" : "Usado";
    public string StatusText => Status switch
    {
        PurchaseStatus.Pending => "Pendente",
        PurchaseStatus.Received => "Recebido",
        PurchaseStatus.Cancelled => "Cancelado",
        _ => "Desconhecido"
    };
    public string DateFormatted => PurchaseDate.ToString("dd/MM/yyyy");
}

public class SupplierItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductForPurchaseViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }

    public string CostFormatted => $"R$ {Cost:N2}";
}
