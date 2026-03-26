namespace Integrations.MercadoLivre.Interfaces;

public interface IMercadoLivreService
{
    Task<List<MeliOrderDto>> GetOrdersAsync();
    Task<List<MeliProductDto>> GetProductsAsync();
    Task UpdateStockAsync(string productId, int quantity);
    Task UpdatePriceAsync(string productId, decimal price);
}

public class MeliOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<MeliOrderItemDto> Items { get; set; } = new();
}

public class MeliOrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class MeliProductDto
{
    public string ProductId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}
