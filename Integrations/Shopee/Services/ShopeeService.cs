using Integrations.Shopee.Interfaces;
using Integrations.Common;

namespace Integrations.Shopee.Services;

public class ShopeeService : BaseApiClient, IShopeeService
{
    private readonly string _partnerId;
    private readonly string _partnerKey;

    public ShopeeService(HttpClient httpClient, string partnerId, string partnerKey) : base(httpClient)
    {
        _partnerId = partnerId;
        _partnerKey = partnerKey;
    }

    public async Task<List<ShopeeOrderDto>> GetOrdersAsync()
    {
        // Shopee API requires complex signing, this is a simplified structure
        // await PostAsync("api/v2/order/get_order_list", new { ... });
        await Task.Delay(100); 
        return new List<ShopeeOrderDto>();
    }

    public async Task<List<ShopeeProductDto>> GetProductsAsync()
    {
        await Task.Delay(100);
        return new List<ShopeeProductDto>();
    }

    public async Task UpdateStockAsync(long productId, int quantity)
    {
        await PostAsync("api/v2/product/update_item_stock", new { item_id = productId, stock_list = new[] { new { normal_stock = quantity } } });
    }

    public async Task UpdatePriceAsync(long productId, decimal price)
    {
        await PostAsync("api/v2/product/update_item_price", new { item_id = productId, price_list = new[] { new { original_price = price } } });
    }
}
