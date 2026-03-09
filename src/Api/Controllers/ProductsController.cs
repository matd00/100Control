using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Products;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly CreateProductUseCase _createProductUseCase;

    public ProductsController(IProductRepository productRepository, CreateProductUseCase createProductUseCase)
    {
        _productRepository = productRepository;
        _createProductUseCase = createProductUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _productRepository.GetAllAsync();
        return Ok(products.Select(p => new ProductDto(p)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(new ProductDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
    {
        try
        {
            var command = new CreateProductCommand
            {
                Name = request.Name,
                Description = request.Description,
                Cost = request.Cost,
                Price = request.Price,
                Weight = request.Weight,
                Width = request.Width,
                Height = request.Height,
                Length = request.Length
            };

            var result = await _createProductUseCase.Execute(command);

            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });

            var product = await _productRepository.GetByIdAsync(result.ProductId);
            if (product == null)
                return BadRequest(new { error = "Product created but not found" });

            return CreatedAtAction(nameof(GetById), new { id = result.ProductId }, new ProductDto(product));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        try
        {
            product.UpdateDetails(request.Name, request.Description);
            product.UpdatePricing(request.Cost, request.Price);
            product.UpdateShippingDimensions(request.Weight, request.Width, request.Height, request.Length);
            
            await _productRepository.UpdateAsync(product);
            return Ok(new ProductDto(product));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        await _productRepository.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/stock/increase")]
    public async Task<ActionResult<ProductDto>> IncreaseStock(Guid id, [FromBody] StockChangeRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        try
        {
            product.IncreaseStock(request.Quantity);
            await _productRepository.UpdateAsync(product);
            return Ok(new ProductDto(product));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/stock/decrease")]
    public async Task<ActionResult<ProductDto>> DecreaseStock(Guid id, [FromBody] StockChangeRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        try
        {
            product.DecreaseStock(request.Quantity);
            await _productRepository.UpdateAsync(product);
            return Ok(new ProductDto(product));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// DTOs
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public int Stock { get; init; }
    public decimal Cost { get; init; }
    public decimal Price { get; init; }
    public decimal Weight { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Length { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    public ProductDto() { }
    public ProductDto(Product product)
    {
        Id = product.Id;
        Name = product.Name;
        Description = product.Description;
        SKU = product.SKU;
        Stock = product.Stock;
        Cost = product.Cost;
        Price = product.Price;
        Weight = product.Weight;
        Width = product.Width;
        Height = product.Height;
        Length = product.Length;
        IsActive = product.IsActive;
        CreatedAt = product.CreatedAt;
    }
}

public record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public decimal Price { get; init; }
    public decimal Weight { get; init; } = 0.3m;
    public int Width { get; init; } = 11;
    public int Height { get; init; } = 2;
    public int Length { get; init; } = 16;
}

public record UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public decimal Price { get; init; }
    public decimal Weight { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Length { get; init; }
}

public record StockChangeRequest
{
    public int Quantity { get; init; }
}
