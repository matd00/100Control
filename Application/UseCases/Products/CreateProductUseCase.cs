using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Products;

public class CreateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public CreateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<CreateProductResult> Execute(CreateProductCommand command)
    {
        try
        {
            if (command == null)
                return CreateProductResult.Failure("Command cannot be null");

            if (string.IsNullOrWhiteSpace(command.Name))
                return CreateProductResult.Failure("Product name is required");

            if (command.Cost < 0 || command.Price <= 0)
                return CreateProductResult.Failure("Cost and Price must be valid");

            var product = new Product(
                command.Name,
                command.Category,
                command.Description,
                command.Cost > 0 ? command.Cost : 0.01m,
                command.Price
            );

            if (command.Weight > 0)
            {
                product.UpdateShippingDimensions(
                    command.Weight,
                    command.Width > 0 ? command.Width : 11,
                    command.Height > 0 ? command.Height : 2,
                    command.Length > 0 ? command.Length : 16
                );
            }

            await _productRepository.SaveAsync(product);

            return CreateProductResult.SuccessResult(product.Id);
        }
        catch (ArgumentException ex)
        {
            return CreateProductResult.Failure(ex.Message);
        }
        catch (Exception)
        {
            return CreateProductResult.Failure("An error occurred while creating the product.");
        }
    }
}

public class CreateProductResult
{
    public bool Success { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }

    public static CreateProductResult SuccessResult(Guid productId) => new() { Success = true, ProductId = productId };
    public static CreateProductResult Failure(string error) => new() { Success = false, ErrorMessage = error };
}

public class CreateProductCommand
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Geral";
    public decimal Cost { get; set; }
    public decimal Price { get; set; }

    // Shipping dimensions
    public decimal Weight { get; set; } = 0.3m;
    public int Width { get; set; } = 11;
    public int Height { get; set; } = 2;
    public int Length { get; set; } = 16;
}
