using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Products;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Models;

namespace Desktop.Features.Products;

public class ProductsViewModel : ViewModelBase
{
    private readonly IProductRepository _productRepository;
    private readonly CreateProductUseCase _createProductUseCase;
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private readonly ISuperFreteService _superFreteService;
    private readonly ISmartSearchService _smartSearchService;

    // Search and Recommendations
    public ObservableCollection<ProductItemViewModel> RecommendedProducts { get; } = new ObservableCollection<ProductItemViewModel>();
    private bool _showRecommendations;
    public bool ShowRecommendations
    {
        get => _showRecommendations;
        set => SetProperty(ref _showRecommendations, value);
    }

    // Product Fields
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _category = "Geral";
    private decimal _cost;
    private decimal _price;
    private int _stockAdjustment;
    private string _adjustmentNotes = string.Empty;

    // Shipping Fields
    private decimal _weight = 0.3m;
    private int _width = 11;
    private int _height = 2;
    private int _length = 16;

    // Freight Calculator
    private string _destinationCep = string.Empty;
    private int _quantity = 1;
    private ProductItemViewModel? _selectedProduct;

    // UI State
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private bool _isCalculatingFreight;
    private bool _isEditing;
    private Guid _editingProductId;

    private string _searchText = string.Empty;

    #region Properties

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterProducts();
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

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public decimal Cost
    {
        get => _cost;
        set => SetProperty(ref _cost, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public int StockAdjustment
    {
        get => _stockAdjustment;
        set => SetProperty(ref _stockAdjustment, value);
    }

    public string AdjustmentNotes
    {
        get => _adjustmentNotes;
        set => SetProperty(ref _adjustmentNotes, value);
    }

    public decimal Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public int Length
    {
        get => _length;
        set => SetProperty(ref _length, value);
    }

    public string DestinationCep
    {
        get => _destinationCep;
        set => SetProperty(ref _destinationCep, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public ProductItemViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value != null)
            {
                // Update freight fields with selected product data
                Weight = value.Weight;
                Width = value.Width;
                Height = value.Height;
                Length = value.Length;
            }
        }
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

    public bool IsCalculatingFreight
    {
        get => _isCalculatingFreight;
        set => SetProperty(ref _isCalculatingFreight, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    #endregion

    public ObservableCollection<ProductItemViewModel> AllProducts { get; } = new();
    public ObservableCollection<ProductItemViewModel> Products { get; } = new();
    public ObservableCollection<FreightQuoteViewModel> FreightQuotes { get; } = new();

    #region Commands

    public ICommand CreateProductCommand { get; }
    public ICommand UpdateProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand ClearFormCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CalculateFreightCommand { get; }
    public ICommand UpdateDimensionsCommand { get; }
    public ICommand SelectProductCommand { get; }
    public ICommand AdjustStockCommand { get; }

    #endregion

    public ProductsViewModel(
        IProductRepository productRepository, 
        CreateProductUseCase createProductUseCase,
        AdjustStockUseCase adjustStockUseCase,
        ISuperFreteService superFreteService,
        ISmartSearchService smartSearchService)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _createProductUseCase = createProductUseCase ?? throw new ArgumentNullException(nameof(createProductUseCase));
        _adjustStockUseCase = adjustStockUseCase ?? throw new ArgumentNullException(nameof(adjustStockUseCase));
        _superFreteService = superFreteService ?? throw new ArgumentNullException(nameof(superFreteService));
        _smartSearchService = smartSearchService ?? throw new ArgumentNullException(nameof(smartSearchService));

        CreateProductCommand = new AsyncRelayCommand(CreateProductAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(Name) && Price > 0);
        UpdateProductCommand = new AsyncRelayCommand(UpdateProductAsync, () => !IsLoading && IsEditing);
        DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, () => !IsLoading && IsEditing);
        ClearFormCommand = new RelayCommand(ClearForm);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        RefreshCommand = new AsyncRelayCommand(LoadProductsAsync);
        CalculateFreightCommand = new AsyncRelayCommand(CalculateFreightAsync, () => !IsCalculatingFreight && SelectedProduct != null && !string.IsNullOrWhiteSpace(DestinationCep));
        UpdateDimensionsCommand = new AsyncRelayCommand(UpdateProductDimensionsAsync, () => SelectedProduct != null);
        SelectProductCommand = new RelayCommand<ProductItemViewModel>(SelectProduct);
        AdjustStockCommand = new AsyncRelayCommand(AdjustStockAsync, () => SelectedProduct != null && StockAdjustment != 0);

        _ = LoadProductsAsync();
    }

    private async Task AdjustStockAsync()
    {
        if (SelectedProduct == null || StockAdjustment == 0) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Ajustando estoque de {SelectedProduct.Name}...";

            await _adjustStockUseCase.Execute(SelectedProduct.Id, StockAdjustment, AdjustmentNotes);

            StatusMessage = "✅ Estoque ajustado com sucesso!";
            StockAdjustment = 0;
            AdjustmentNotes = string.Empty;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao ajustar estoque: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectProduct(ProductItemViewModel? product)
    {
        if (product != null)
        {
            SelectedProduct = product;

            // Preencher formulário para edição
            _editingProductId = product.Id;
            Name = product.Name;
            Description = product.Description;
            Category = product.Category;
            Cost = product.Cost;
            Price = product.Price;
            Weight = product.Weight;
            Width = product.Width;
            Height = product.Height;
            Length = product.Length;
            IsEditing = true;

            StatusMessage = $"Produto selecionado: {product.Name}";
        }
    }

    private async Task CreateProductAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Criando produto...";

            var command = new CreateProductCommand
            {
                Name = Name,
                Description = Description,
                Category = Category,
                Cost = Cost,
                Price = Price,
                Weight = Weight,
                Width = Width,
                Height = Height,
                Length = Length
            };

            var result = await _createProductUseCase.Execute(command);

            if (!result.Success)
            {
                StatusMessage = $"❌ Erro: {result.ErrorMessage}";
                return;
            }

            StatusMessage = "✅ Produto criado com sucesso!";
            ClearForm();
            await LoadProductsAsync();
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        catch (Exception)
        {
            StatusMessage = "❌ Erro ao criar produto. Tente novamente.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateProductAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Atualizando produto...";

            var product = await _productRepository.GetByIdAsync(_editingProductId);
            if (product == null)
            {
                StatusMessage = "❌ Produto não encontrado.";
                return;
            }

            product.UpdateDetails(Name, Description, Category);
            product.UpdatePricing(Cost, Price);
            product.UpdateShippingDimensions(Weight, Width, Height, Length);

            await _productRepository.UpdateAsync(product);

            StatusMessage = "✅ Produto atualizado com sucesso!";
            ClearForm();
            await LoadProductsAsync();
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        catch (Exception)
        {
            StatusMessage = "❌ Erro ao atualizar produto.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteProductAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Excluindo produto...";

            await _productRepository.DeleteAsync(_editingProductId);

            StatusMessage = "✅ Produto excluído com sucesso!";
            ClearForm();
            await LoadProductsAsync();
        }
        catch (Exception)
        {
            StatusMessage = "❌ Erro ao excluir produto.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CalculateFreightAsync()
    {
        if (SelectedProduct == null || string.IsNullOrWhiteSpace(DestinationCep))
        {
            StatusMessage = "❌ Selecione um produto e informe o CEP de destino.";
            return;
        }

        try
        {
            IsCalculatingFreight = true;
            FreightQuotes.Clear();
            StatusMessage = "🚚 Calculando frete...";

            var request = new FreightQuoteRequest
            {
                DestinationPostalCode = DestinationCep.Replace("-", ""),
                Weight = SelectedProduct.Weight,
                Width = SelectedProduct.Width,
                Height = SelectedProduct.Height,
                Length = SelectedProduct.Length,
                Quantity = Quantity
            };

            var quotes = await _superFreteService.GetAllQuotesAsync(request);

            foreach (var quote in quotes.Where(q => string.IsNullOrEmpty(q.Error)).OrderBy(q => q.Price))
            {
                FreightQuotes.Add(new FreightQuoteViewModel
                {
                    ServiceName = quote.Name ?? "Serviço",
                    CompanyName = quote.Company?.Name ?? "Transportadora",
                    Price = quote.Price,
                    DeliveryTime = quote.DeliveryTime,
                    DeliveryRange = $"{quote.DeliveryRange?.Min ?? quote.DeliveryTime} a {quote.DeliveryRange?.Max ?? quote.DeliveryTime} dias úteis"
                });
            }

            StatusMessage = FreightQuotes.Count > 0 
                ? $"✅ {FreightQuotes.Count} opções de frete encontradas" 
                : "⚠️ Nenhuma opção de frete disponível para este CEP";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao calcular frete: {ex.Message}";
        }
        finally
        {
            IsCalculatingFreight = false;
        }
    }

    private async Task UpdateProductDimensionsAsync()
    {
        if (SelectedProduct == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Atualizando dimensões...";

            var product = await _productRepository.GetByIdAsync(SelectedProduct.Id);
            if (product == null)
            {
                StatusMessage = "❌ Produto não encontrado.";
                return;
            }

            product.UpdateShippingDimensions(Weight, Width, Height, Length);
            await _productRepository.UpdateAsync(product);

            // Update ViewModel
            SelectedProduct.Weight = Weight;
            SelectedProduct.Width = Width;
            SelectedProduct.Height = Height;
            SelectedProduct.Length = Length;

            StatusMessage = "✅ Dimensões atualizadas com sucesso!";
            await LoadProductsAsync();
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        catch (Exception)
        {
            StatusMessage = "❌ Erro ao atualizar dimensões.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            IsLoading = true;
            var products = await _productRepository.GetAllAsync();

            AllProducts.Clear();
            foreach (var product in products.OrderByDescending(p => p.CreatedAt))
            {
                AllProducts.Add(new ProductItemViewModel
                {
                    Id = product.Id,
                    SKU = product.SKU,
                    Name = product.Name,
                    Description = product.Description,
                    Category = product.Category,
                    Stock = product.Stock,
                    Cost = product.Cost,
                    Price = product.Price,
                    Weight = product.Weight,
                    Width = product.Width,
                    Height = product.Height,
                    Length = product.Length,
                    IsActive = product.IsActive
                });
            }
            FilterProducts();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao carregar produtos: {ex.Message}";
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
        Cost = 0;
        Price = 0;
        Weight = 0.3m;
        Width = 11;
        Height = 2;
        Length = 16;
        IsEditing = false;
        SelectedProduct = null;
        _editingProductId = Guid.Empty;
    }

    private void FilterProducts()
    {
        Products.Clear();
        RecommendedProducts.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var p in AllProducts) Products.Add(p);
            ShowRecommendations = false;
            return;
        }

        // Use Smart Search for Products
        var filtered = _smartSearchService.Search(
            AllProducts, 
            SearchText, 
            p => $"{p.Name} {p.SKU} {p.Category}", 
            threshold: 0.25);

        foreach (var p in filtered)
        {
            Products.Add(p);
        }

        // Recommendations (Top 5 highly relevant)
        var recommendations = filtered.Take(5).ToList();
        if (recommendations.Any() && SearchText.Length > 2)
        {
            foreach (var p in recommendations) RecommendedProducts.Add(p);
            ShowRecommendations = true;
        }
        else
        {
            ShowRecommendations = false;
        }
    }
}

public class ProductItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal Weight { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Length { get; set; }
    public bool IsActive { get; set; }

    public string Dimensions => $"{Width}x{Height}x{Length} cm";
    public string WeightFormatted => $"{Weight:N2} kg";
    public string PriceFormatted => $"R$ {Price:N2}";
    public string CostFormatted => $"R$ {Cost:N2}";
    public string StockStatus => Stock <= 5 ? "BAIXO" : "OK";
    public bool IsLowStock => Stock <= 5;
}

public class FreightQuoteViewModel
{
    public string ServiceName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DeliveryTime { get; set; }
    public string DeliveryRange { get; set; } = string.Empty;

    public string PriceFormatted => $"R$ {Price:N2}";
}
