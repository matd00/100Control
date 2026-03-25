using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Products;

public class CreateProductUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Execute(CreateProductCommand command)
    {
        try
        {
            if (command == null)
                return Result<Guid>.Failure("Command cannot be null");

            if (string.IsNullOrWhiteSpace(command.Name))
                return Result<Guid>.Failure("Product name is required");

            if (command.Cost < 0 || command.Price <= 0)
                return Result<Guid>.Failure("Cost and Price must be valid");

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
            await _unitOfWork.SaveChangesAsync();

            return Result<Guid>.Success(product.Id);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return Result<Guid>.Failure("An error occurred while creating the product.");
        }
    }
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
