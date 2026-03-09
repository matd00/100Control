using Microsoft.AspNetCore.Mvc;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Models;
using Domain.Interfaces.Repositories;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly ISuperFreteService _superFreteService;
    private readonly IProductRepository _productRepository;

    public ShippingController(ISuperFreteService superFreteService, IProductRepository productRepository)
    {
        _superFreteService = superFreteService;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Calculate shipping quotes for a product
    /// </summary>
    [HttpPost("quote")]
    public async Task<ActionResult<IEnumerable<ShippingQuoteDto>>> CalculateQuote([FromBody] ShippingQuoteRequest request)
    {
        try
        {
            var freightRequest = new FreightQuoteRequest
            {
                DestinationPostalCode = request.DestinationCep.Replace("-", ""),
                Weight = request.Weight,
                Width = request.Width,
                Height = request.Height,
                Length = request.Length,
                Quantity = request.Quantity
            };

            var quotes = await _superFreteService.GetAllQuotesAsync(freightRequest);

            var result = quotes
                .Where(q => string.IsNullOrEmpty(q.Error))
                .OrderBy(q => q.Price)
                .Select(q => new ShippingQuoteDto
                {
                    ServiceName = q.Name ?? "Serviço",
                    CompanyName = q.Company?.Name ?? "Transportadora",
                    Price = q.Price,
                    DeliveryTimeMin = q.DeliveryRange?.Min ?? q.DeliveryTime,
                    DeliveryTimeMax = q.DeliveryRange?.Max ?? q.DeliveryTime,
                    Currency = "BRL"
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate shipping for a specific product by ID
    /// </summary>
    [HttpPost("quote/product/{productId}")]
    public async Task<ActionResult<IEnumerable<ShippingQuoteDto>>> CalculateQuoteForProduct(Guid productId, [FromBody] ProductShippingRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
            return NotFound(new { error = "Product not found" });

        try
        {
            var freightRequest = new FreightQuoteRequest
            {
                DestinationPostalCode = request.DestinationCep.Replace("-", ""),
                Weight = product.Weight,
                Width = product.Width,
                Height = product.Height,
                Length = product.Length,
                Quantity = request.Quantity
            };

            var quotes = await _superFreteService.GetAllQuotesAsync(freightRequest);

            var result = quotes
                .Where(q => string.IsNullOrEmpty(q.Error))
                .OrderBy(q => q.Price)
                .Select(q => new ShippingQuoteDto
                {
                    ServiceName = q.Name ?? "Serviço",
                    CompanyName = q.Company?.Name ?? "Transportadora",
                    Price = q.Price,
                    DeliveryTimeMin = q.DeliveryRange?.Min ?? q.DeliveryTime,
                    DeliveryTimeMax = q.DeliveryRange?.Max ?? q.DeliveryTime,
                    Currency = "BRL"
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record ShippingQuoteRequest
{
    public string DestinationCep { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Length { get; init; }
    public int Quantity { get; init; } = 1;
}

public record ProductShippingRequest
{
    public string DestinationCep { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
}

public record ShippingQuoteDto
{
    public string ServiceName { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DeliveryTimeMin { get; init; }
    public int DeliveryTimeMax { get; init; }
    public string Currency { get; init; } = "BRL";
    public string DeliveryRange => $"{DeliveryTimeMin} a {DeliveryTimeMax} dias úteis";
}
