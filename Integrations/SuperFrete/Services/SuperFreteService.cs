using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
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
            PropertyNameCaseInsensitive = true,
            WriteIndented = true // Para melhor visualização no log
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

        var responseContent = await response.Content.ReadAsStringAsync();

        // Log das cotações para debug
        var logMessage = new StringBuilder();
        logMessage.AppendLine("========== SUPERFRETE QUOTES DEBUG ==========");
        logMessage.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logMessage.AppendLine($"Response JSON: {responseContent}");
        logMessage.AppendLine("==============================================");
        Debug.WriteLine(logMessage.ToString());
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "superfrete_debug.log"), 
            logMessage.ToString() + Environment.NewLine);

        var quotes = JsonSerializer.Deserialize<List<SuperFreteQuoteResponse>>(responseContent, _jsonOptions);
        return quotes ?? new List<SuperFreteQuoteResponse>();
    }

    public async Task<string> GenerateLabelAsync(ShipmentLabelRequest request)
    {
        // Validação dos dados obrigatórios
        ValidateRequest(request);

        // Monta a lista de produtos
        var products = request.Products.Select(p => new SuperFreteProduct
        {
            Name = !string.IsNullOrWhiteSpace(p.Name) ? p.Name : "Produto",
            Quantity = p.Quantity > 0 ? p.Quantity : 1,
            UnitaryValue = p.UnitPrice > 0 ? p.UnitPrice : 10.00m
        }).ToList();

        // Se não tiver produtos, cria um genérico
        if (products.Count == 0)
        {
            products.Add(new SuperFreteProduct
            {
                Name = "Produto",
                Quantity = 1,
                UnitaryValue = request.ShippingPrice > 0 ? request.ShippingPrice : 10.00m
            });
        }

        // Monta o volume (obrigatório)
        var volumes = new List<SuperFreteVolume>
        {
            new SuperFreteVolume
            {
                Weight = request.Weight > 0 ? request.Weight : 0.3m,
                Width = request.Width > 0 ? request.Width : 11,
                Height = request.Height > 0 ? request.Height : 2,
                Length = request.Length > 0 ? request.Length : 16
            }
        };

        // Extrai número do endereço se não foi informado separadamente
        var (address, number) = ExtractAddressNumber(request.ReceiverAddress, request.ReceiverNumber);

        var apiRequest = new SuperFreteShipmentRequest
        {
            From = new SuperFreteAddress
            {
                PostalCode = _settings.DefaultOriginPostalCode.Replace("-", ""),
                Name = "Loja 100Control",
                Phone = "11999999999",
                Address = "Rua Principal",
                Number = "100",
                District = "Centro",
                City = "Limeira",
                StateAbbr = "SP"
            },
            To = new SuperFreteAddress
            {
                PostalCode = CleanPostalCode(request.ReceiverZipCode),
                Name = !string.IsNullOrWhiteSpace(request.ReceiverName) ? request.ReceiverName : "Destinatário",
                Phone = CleanPhone(request.ReceiverPhone),
                Email = !string.IsNullOrWhiteSpace(request.ReceiverEmail) ? request.ReceiverEmail : "cliente@email.com",
                Address = !string.IsNullOrWhiteSpace(address) ? address : "Endereço não informado",
                Number = !string.IsNullOrWhiteSpace(number) ? number : "S/N",
                Complement = request.ReceiverComplement ?? "",
                District = !string.IsNullOrWhiteSpace(request.ReceiverDistrict) ? request.ReceiverDistrict : "Centro",
                City = !string.IsNullOrWhiteSpace(request.ReceiverCity) ? request.ReceiverCity : "Cidade",
                StateAbbr = !string.IsNullOrWhiteSpace(request.ReceiverState) ? request.ReceiverState : "SP"
            },
            Products = products,
            Volumes = volumes,
            Options = new SuperFreteOptions
            {
                InsuranceValue = request.ShippingPrice > 0 ? request.ShippingPrice : 0,
                Receipt = false,
                OwnHand = false,
                NonCommercial = false
            },
            Service = request.ServiceId > 0 ? request.ServiceId : 1
        };

        var json = JsonSerializer.Serialize(apiRequest, _jsonOptions);

        // === LOG DETALHADO PARA DEBUG ===
        var logMessage = new StringBuilder();
        logMessage.AppendLine("========== SUPERFRETE API REQUEST DEBUG ==========");
        logMessage.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logMessage.AppendLine($"Endpoint: POST /api/v0/cart");
        logMessage.AppendLine($"Base URL: {_settings.BaseUrl}");
        logMessage.AppendLine();
        logMessage.AppendLine("--- DADOS RECEBIDOS (ShipmentLabelRequest) ---");
        logMessage.AppendLine($"ReceiverName: '{request.ReceiverName}'");
        logMessage.AppendLine($"ReceiverPhone: '{request.ReceiverPhone}'");
        logMessage.AppendLine($"ReceiverEmail: '{request.ReceiverEmail}'");
        logMessage.AppendLine($"ReceiverAddress: '{request.ReceiverAddress}'");
        logMessage.AppendLine($"ReceiverNumber: '{request.ReceiverNumber}'");
        logMessage.AppendLine($"ReceiverComplement: '{request.ReceiverComplement}'");
        logMessage.AppendLine($"ReceiverDistrict: '{request.ReceiverDistrict}'");
        logMessage.AppendLine($"ReceiverCity: '{request.ReceiverCity}'");
        logMessage.AppendLine($"ReceiverState: '{request.ReceiverState}'");
        logMessage.AppendLine($"ReceiverZipCode: '{request.ReceiverZipCode}'");
        logMessage.AppendLine($"Weight: {request.Weight}");
        logMessage.AppendLine($"Width: {request.Width}");
        logMessage.AppendLine($"Height: {request.Height}");
        logMessage.AppendLine($"Length: {request.Length}");
        logMessage.AppendLine($"ServiceId: {request.ServiceId}");
        logMessage.AppendLine($"Products Count: {request.Products?.Count ?? 0}");
        logMessage.AppendLine();
        logMessage.AppendLine("--- JSON ENVIADO PARA API ---");
        logMessage.AppendLine(json);
        logMessage.AppendLine("===============================================");

        Debug.WriteLine(logMessage.ToString());
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "superfrete_debug.log"), 
            logMessage.ToString() + Environment.NewLine);
        // === FIM DO LOG ===

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v0/cart", content);

        // Log da resposta
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseLog = new StringBuilder();
        responseLog.AppendLine("========== SUPERFRETE API RESPONSE ==========");
        responseLog.AppendLine($"Status Code: {response.StatusCode}");
        responseLog.AppendLine($"Response: {responseContent}");
        responseLog.AppendLine("==============================================");
        Debug.WriteLine(responseLog.ToString());
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "superfrete_debug.log"), 
            responseLog.ToString() + Environment.NewLine);

        if (!response.IsSuccessStatusCode)
        {
            throw new SuperFreteException($"Erro ao gerar etiqueta: {response.StatusCode} - {responseContent}");
        }

        var shipment = JsonSerializer.Deserialize<SuperFreteShipmentResponse>(responseContent, _jsonOptions);

        if (shipment == null || !string.IsNullOrEmpty(shipment.Error))
        {
            throw new SuperFreteException($"Erro ao gerar etiqueta: {shipment?.Message ?? shipment?.Error ?? "Erro desconhecido"}");
        }

        return shipment.Tracking ?? shipment.Id ?? throw new SuperFreteException("Código de rastreio não retornado pela API.");
    }

    private static void ValidateRequest(ShipmentLabelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiverZipCode))
            throw new SuperFreteException("CEP do destinatário é obrigatório.");
    }

    private static string CleanPostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return "00000000";
        return postalCode.Replace("-", "").Replace(".", "").Replace(" ", "").Trim();
    }

    private static string CleanPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "11999999999";
        return phone.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
    }

    private static (string address, string number) ExtractAddressNumber(string fullAddress, string number)
    {
        // Se já tem número, retorna como está
        if (!string.IsNullOrWhiteSpace(number))
            return (fullAddress ?? "", number);

        if (string.IsNullOrWhiteSpace(fullAddress))
            return ("", "S/N");

        // Tenta extrair número do endereço (ex: "Rua das Flores, 123" ou "Rua das Flores 123")
        var parts = fullAddress.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var possibleNumber = parts[1].Trim().Split(' ')[0];
            if (int.TryParse(possibleNumber, out _))
            {
                return (parts[0].Trim(), possibleNumber);
            }
        }

        // Tenta encontrar número no final do endereço
        var words = fullAddress.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = words.Length - 1; i >= 0; i--)
        {
            if (int.TryParse(words[i].Replace(",", ""), out _))
            {
                var addressPart = string.Join(" ", words.Take(i));
                return (addressPart, words[i].Replace(",", ""));
            }
        }

        return (fullAddress, "S/N");
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
