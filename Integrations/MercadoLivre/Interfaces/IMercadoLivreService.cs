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
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public DateTime CreatedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<MeliOrderItemDto> Items { get; set; }
}

public class MeliOrderItemDto
{
    public string ProductId { get; set; }
    public string Title { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class MeliProductDto
{
    public string ProductId { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}
