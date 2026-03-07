using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Products;

namespace Desktop.Features.Products;

public class ProductsViewModel : ViewModelBase
{
    private readonly IProductRepository _productRepository;
    private readonly CreateProductUseCase _createProductUseCase;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private decimal _cost;
    private decimal _price;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

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

    public ObservableCollection<ProductItemViewModel> Products { get; } = new();

    public ICommand CreateProductCommand { get; }
    public ICommand RefreshCommand { get; }

    public ProductsViewModel(IProductRepository productRepository, CreateProductUseCase createProductUseCase)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _createProductUseCase = createProductUseCase ?? throw new ArgumentNullException(nameof(createProductUseCase));

        CreateProductCommand = new AsyncRelayCommand(CreateProductAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(Name));
        RefreshCommand = new AsyncRelayCommand(LoadProductsAsync);

        _ = LoadProductsAsync();
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
                Cost = Cost,
                Price = Price
            };

            await _createProductUseCase.Execute(command);

            StatusMessage = "Produto criado com sucesso!";
            ClearForm();
            await LoadProductsAsync();
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        catch (Exception)
        {
            StatusMessage = "Erro ao criar produto. Tente novamente.";
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

            Products.Clear();
            foreach (var product in products.OrderByDescending(p => p.CreatedAt))
            {
                Products.Add(new ProductItemViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Stock = product.Stock,
                    Cost = product.Cost,
                    Price = product.Price,
                    IsActive = product.IsActive
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao carregar produtos: {ex.Message}";
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
    }
}

public class ProductItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
