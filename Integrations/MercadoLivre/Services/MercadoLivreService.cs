using Integrations.MercadoLivre.Interfaces;

namespace Integrations.MercadoLivre.Services;

public class MercadoLivreService : IMercadoLivreService
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public MercadoLivreService(HttpClient httpClient, string accessToken)
    {
        _httpClient = httpClient;
        _accessToken = accessToken;
    }

    public async Task<List<MeliOrderDto>> GetOrdersAsync()
    {
        // TODO: Implement API call to Mercado Livre
        // GET https://api.mercadolibre.com/orders/search
        await Task.Delay(100); // Placeholder
        return new List<MeliOrderDto>();
    }

    public async Task<List<MeliProductDto>> GetProductsAsync()
    {
        // TODO: Implement API call to Mercado Livre
        // GET https://api.mercadolibre.com/users/me/listings
        await Task.Delay(100); // Placeholder
        return new List<MeliProductDto>();
    }

    public async Task UpdateStockAsync(string productId, int quantity)
    {
        // TODO: Implement API call to Mercado Livre
        // PUT https://api.mercadolibre.com/items/{itemId}
        await Task.Delay(100); // Placeholder
    }

    public async Task UpdatePriceAsync(string productId, decimal price)
    {
        // TODO: Implement API call to Mercado Livre
        // PUT https://api.mercadolibre.com/items/{itemId}
        await Task.Delay(100); // Placeholder
    }
}
