using Integrations.Shopee.Interfaces;

namespace Integrations.Shopee.Services;

public class ShopeeService : IShopeeService
{
    private readonly HttpClient _httpClient;
    private readonly string _partnerId;
    private readonly string _partnerKey;

    public ShopeeService(HttpClient httpClient, string partnerId, string partnerKey)
    {
        _httpClient = httpClient;
        _partnerId = partnerId;
        _partnerKey = partnerKey;
    }

    public async Task<List<ShopeeOrderDto>> GetOrdersAsync()
    {
        // TODO: Implement API call to Shopee
        // POST https://partner.shopeemobile.com/api/v2/order/get_order_list
        await Task.Delay(100); // Placeholder
        return new List<ShopeeOrderDto>();
    }

    public async Task<List<ShopeeProductDto>> GetProductsAsync()
    {
        // TODO: Implement API call to Shopee
        // POST https://partner.shopeemobile.com/api/v2/product/get_item_list
        await Task.Delay(100); // Placeholder
        return new List<ShopeeProductDto>();
    }

    public async Task UpdateStockAsync(long productId, int quantity)
    {
        // TODO: Implement API call to Shopee
        // POST https://partner.shopeemobile.com/api/v2/product/update_item_stock
        await Task.Delay(100); // Placeholder
    }

    public async Task UpdatePriceAsync(long productId, decimal price)
    {
        // TODO: Implement API call to Shopee
        // POST https://partner.shopeemobile.com/api/v2/product/update_item_price
        await Task.Delay(100); // Placeholder
    }
}
