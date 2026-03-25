using Integrations.MercadoLivre.Interfaces;
using Integrations.Common;
using System.Net.Http.Headers;

namespace Integrations.MercadoLivre.Services;

public class MercadoLivreService : BaseApiClient, IMercadoLivreService
{
    private readonly string _accessToken;

    public MercadoLivreService(HttpClient httpClient, string accessToken) : base(httpClient)
    {
        _accessToken = accessToken;
        if (!string.IsNullOrEmpty(_accessToken))
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    public async Task<List<MeliOrderDto>> GetOrdersAsync()
    {
        // Real implementation would use GetAsync<List<MeliOrderDto>>("orders/search")
        await Task.Delay(100); 
        return new List<MeliOrderDto>();
    }

    public async Task<List<MeliProductDto>> GetProductsAsync()
    {
        // Real implementation would use GetAsync<List<MeliProductDto>>("users/me/listings")
        await Task.Delay(100);
        return new List<MeliProductDto>();
    }

    public async Task UpdateStockAsync(string productId, int quantity)
    {
        await PutAsync($"items/{productId}", new { available_quantity = quantity });
    }

    public async Task UpdatePriceAsync(string productId, decimal price)
    {
        await PutAsync($"items/{productId}", new { price = price });
    }
}
