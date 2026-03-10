using Integrations.SuperFrete.Models;

namespace Integrations.SuperFrete.Interfaces;

public interface ISuperFreteService
{
    Task<decimal> CalculateFreightAsync(FreightQuoteRequest request);
    Task<List<SuperFreteQuoteResponse>> GetAllQuotesAsync(FreightQuoteRequest request);
    Task<string> GenerateLabelAsync(ShipmentLabelRequest request);
    Task<ShipmentTrackingDto> TrackShipmentAsync(string trackingNumber);
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
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public string ReceiverState { get; set; } = string.Empty;
    public string ReceiverZipCode { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Width { get; set; } = 11;
    public int Height { get; set; } = 2;
    public int Length { get; set; } = 16;
    public int ServiceId { get; set; } = 1; // 1 = SEDEX, 2 = PAC, etc.
    public string ServiceName { get; set; } = string.Empty;
    public decimal ShippingPrice { get; set; }
}

public class ShipmentTrackingDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; }
    public string Location { get; set; } = string.Empty;
}
