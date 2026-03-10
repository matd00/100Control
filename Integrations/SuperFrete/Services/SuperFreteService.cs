using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Integrations.SuperFrete.Configuration;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Models;

namespace Integrations.SuperFrete.Services;

public class SuperFreteService : ISuperFreteService
{
    private readonly HttpClient _httpClient;
    private readonly SuperFreteSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    public SuperFreteService(HttpClient httpClient, SuperFreteSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "100Control/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<decimal> CalculateFreightAsync(FreightQuoteRequest request)
    {
        var apiRequest = new SuperFreteQuoteRequest
        {
            From = new SuperFreteAddress
            {
                PostalCode = _settings.DefaultOriginPostalCode.Replace("-", "")
            },
            To = new SuperFreteAddress
            {
                PostalCode = request.DestinationPostalCode.Replace("-", "")
            },
            Package = new SuperFretePackage
            {
                Weight = request.TotalWeight,
                Width = request.Width,
                Height = request.Height,
                Length = request.Length
            }
        };

        var json = JsonSerializer.Serialize(apiRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v0/calculator", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new SuperFreteException($"Erro ao calcular frete: {response.StatusCode} - {errorContent}");
        }

        var quotes = await response.Content.ReadFromJsonAsync<List<SuperFreteQuoteResponse>>(_jsonOptions);

        if (quotes == null || quotes.Count == 0)
        {
            throw new SuperFreteException("Nenhuma opção de frete disponível para este destino.");
        }

        // Retorna o menor preço disponível
        return quotes.Where(q => string.IsNullOrEmpty(q.Error)).Min(q => q.Price);
    }

    public async Task<List<SuperFreteQuoteResponse>> GetAllQuotesAsync(FreightQuoteRequest request)
    {
        var apiRequest = new SuperFreteQuoteRequest
        {
            From = new SuperFreteAddress
            {
                PostalCode = _settings.DefaultOriginPostalCode.Replace("-", "")
            },
            To = new SuperFreteAddress
            {
                PostalCode = request.DestinationPostalCode.Replace("-", "")
            },
            Package = new SuperFretePackage
            {
                Weight = request.TotalWeight,
                Width = request.Width,
                Height = request.Height,
                Length = request.Length
            }
        };

        var json = JsonSerializer.Serialize(apiRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v0/calculator", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new SuperFreteException($"Erro ao calcular frete: {response.StatusCode} - {errorContent}");
        }

        var quotes = await response.Content.ReadFromJsonAsync<List<SuperFreteQuoteResponse>>(_jsonOptions);
        return quotes ?? new List<SuperFreteQuoteResponse>();
    }

    public async Task<string> GenerateLabelAsync(ShipmentLabelRequest request)
    {
        var apiRequest = new SuperFreteShipmentRequest
        {
            From = new SuperFreteAddress
            {
                PostalCode = _settings.DefaultOriginPostalCode.Replace("-", "")
            },
            To = new SuperFreteAddress
            {
                PostalCode = request.ReceiverZipCode.Replace("-", ""),
                Name = request.ReceiverName,
                Address = request.ReceiverAddress,
                City = request.ReceiverCity,
                StateAbbr = request.ReceiverState
            },
            Package = new SuperFretePackage
            {
                Weight = request.Weight,
                Width = request.Width > 0 ? request.Width : 11,
                Height = request.Height > 0 ? request.Height : 2,
                Length = request.Length > 0 ? request.Length : 16
            },
            Service = request.ServiceId > 0 ? request.ServiceId : 1
        };

        var json = JsonSerializer.Serialize(apiRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v0/cart", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new SuperFreteException($"Erro ao gerar etiqueta: {response.StatusCode} - {errorContent}");
        }

        var shipment = await response.Content.ReadFromJsonAsync<SuperFreteShipmentResponse>(_jsonOptions);

        if (shipment == null || !string.IsNullOrEmpty(shipment.Error))
        {
            throw new SuperFreteException($"Erro ao gerar etiqueta: {shipment?.Message ?? shipment?.Error ?? "Erro desconhecido"}");
        }

        return shipment.Tracking ?? shipment.Id ?? throw new SuperFreteException("Código de rastreio não retornado pela API.");
    }

    public async Task<ShipmentTrackingDto> TrackShipmentAsync(string trackingNumber)
    {
        var response = await _httpClient.GetAsync($"/api/v0/tracking/{trackingNumber}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new SuperFreteException($"Erro ao rastrear envio: {response.StatusCode} - {errorContent}");
        }

        var tracking = await response.Content.ReadFromJsonAsync<SuperFreteTrackingResponse>(_jsonOptions);

        if (tracking == null)
        {
            throw new SuperFreteException("Resposta inválida da API de rastreamento.");
        }

        var lastEvent = tracking.Events?.OrderByDescending(e => e.Date).FirstOrDefault();

        return new ShipmentTrackingDto
        {
            TrackingNumber = tracking.Tracking ?? trackingNumber,
            Status = tracking.Status ?? "Desconhecido",
            LastUpdate = lastEvent?.Date ?? DateTime.UtcNow,
            Location = lastEvent?.Location ?? "Não informado"
        };
    }
}

public class SuperFreteException : Exception
{
    public SuperFreteException(string message) : base(message) { }
    public SuperFreteException(string message, Exception innerException) : base(message, innerException) { }
}
