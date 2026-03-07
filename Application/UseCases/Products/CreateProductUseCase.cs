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

    public async Task Execute(CreateProductCommand command)
    {
        try
        {
            // Security: Input validation
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Product name is required", nameof(command.Name));

            if (command.Cost <= 0 || command.Price <= 0)
                throw new ArgumentException("Cost and Price must be greater than 0");

            var product = new Product(
                command.Name,
                command.Description,
                command.Cost,
                command.Price
            );

            await _productRepository.SaveAsync(product);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Security: Don't expose internal exception details
            throw new InvalidOperationException("An error occurred while creating the product. Please try again later.");
        }
    }
}

public class CreateProductCommand
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
}
