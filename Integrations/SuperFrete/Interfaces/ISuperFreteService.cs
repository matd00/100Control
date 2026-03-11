using Integrations.SuperFrete.Models;

namespace Integrations.SuperFrete.Interfaces;

public interface ISuperFreteService
{
    Task<decimal> CalculateFreightAsync(FreightQuoteRequest request);
    Task<List<SuperFreteQuoteResponse>> GetAllQuotesAsync(FreightQuoteRequest request);
    Task<ShipmentResult> GenerateLabelAsync(ShipmentLabelRequest request);
    Task<ShipmentResult> CheckoutAsync(string orderId);
    Task<string?> GetLabelUrlAsync(string orderId);
    Task<ShipmentTrackingDto> TrackShipmentAsync(string trackingNumber);
    Task CancelOrderAsync(string superFreteOrderId);
}

/// <summary>
/// Resultado da geração de etiqueta com todas as informações necessárias
/// </summary>
public class ShipmentResult
{
    public string OrderId { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public string? LabelUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal ShippingCost { get; set; }
}

public class FreightQuoteRequest
{
    public string DestinationPostalCode { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Width { get; set; } = 11;   // cm (min Correios)
    public int Height { get; set; } = 2;   // cm (min Correios)
    public int Length { get; set; } = 16;  // cm (min Correios)
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Calcula peso total baseado na quantidade
    /// </summary>
    public decimal TotalWeight => Weight * Quantity;
}

public class ShipmentLabelRequest
{
    // Dados do destinatário
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverDocument { get; set; } = string.Empty; // CPF ou CNPJ
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverEmail { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string ReceiverNumber { get; set; } = string.Empty;
    public string ReceiverComplement { get; set; } = string.Empty;
    public string ReceiverDistrict { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public string ReceiverState { get; set; } = string.Empty;
    public string ReceiverZipCode { get; set; } = string.Empty;

    // Dimensões do pacote
    public decimal Weight { get; set; }
    public int Width { get; set; } = 11;
    public int Height { get; set; } = 2;
    public int Length { get; set; } = 16;

    // Serviço de envio
    public int ServiceId { get; set; } = 1;
    public string ServiceName { get; set; } = string.Empty;
    public decimal ShippingPrice { get; set; }

    // Produtos
    public List<ShipmentProduct> Products { get; set; } = new();
}

public class ShipmentProduct
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class ShipmentTrackingDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; }
    public string Location { get; set; } = string.Empty;
}
