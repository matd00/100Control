namespace Integrations.Shopee.Interfaces;

public interface IShopeeService
{
    Task<List<ShopeeOrderDto>> GetOrdersAsync();
    Task<List<ShopeeProductDto>> GetProductsAsync();
    Task UpdateStockAsync(long productId, int quantity);
    Task UpdatePriceAsync(long productId, decimal price);
}

public class ShopeeOrderDto
{
    public string OrderId { get; set; }
    public string BuyerUsername { get; set; }
    public string BuyerEmail { get; set; }
    public DateTime CreatedTime { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ShopeeOrderItemDto> Items { get; set; }
}

public class ShopeeOrderItemDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class ShopeeProductDto
{
    public long ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
