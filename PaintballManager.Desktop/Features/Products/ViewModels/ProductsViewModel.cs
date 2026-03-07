using PaintballManager.Application.UseCases.Products;
using PaintballManager.Desktop.Infrastructure.MVVM;

namespace PaintballManager.Desktop.Features.Products.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly CreateProductUseCase _createProductUseCase;

    private List<ProductItemViewModel> _products;
    public List<ProductItemViewModel> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    private string _productName;
    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    private string _productDescription;
    public string ProductDescription
    {
        get => _productDescription;
        set => SetProperty(ref _productDescription, value);
    }

    private decimal _productCost;
    public decimal ProductCost
    {
        get => _productCost;
        set => SetProperty(ref _productCost, value);
    }

    private decimal _productPrice;
    public decimal ProductPrice
    {
        get => _productPrice;
        set => SetProperty(ref _productPrice, value);
    }

    public RelayCommand CreateProductCommand { get; }

    public ProductsViewModel(CreateProductUseCase createProductUseCase)
    {
        _createProductUseCase = createProductUseCase;
        Products = new List<ProductItemViewModel>();

        CreateProductCommand = new RelayCommand(async _ => await CreateProduct(), _ => CanCreateProduct());
    }

    private async Task CreateProduct()
    {
        var command = new CreateProductCommand
        {
            Name = ProductName,
            Description = ProductDescription,
            Cost = ProductCost,
            Price = ProductPrice
        };

        try
        {
            await _createProductUseCase.Execute(command);
            ResetForm();
        }
        catch (Exception ex)
        {
            // Show error
            System.Diagnostics.Debug.WriteLine($"Error creating product: {ex.Message}");
        }
    }

    private bool CanCreateProduct()
    {
        return !string.IsNullOrWhiteSpace(ProductName) && ProductCost > 0 && ProductPrice > 0;
    }

    private void ResetForm()
    {
        ProductName = string.Empty;
        ProductDescription = string.Empty;
        ProductCost = 0;
        ProductPrice = 0;
    }
}

public class ProductItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Stock { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
