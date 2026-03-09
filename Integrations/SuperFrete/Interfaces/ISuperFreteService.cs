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
    public string TrackingNumber { get; set; }
    public string ReceiverName { get; set; }
    public string ReceiverAddress { get; set; }
    public string ReceiverCity { get; set; }
    public string ReceiverState { get; set; }
    public string ReceiverZipCode { get; set; }
    public decimal Weight { get; set; }
}

public class ShipmentTrackingDto
{
    public string TrackingNumber { get; set; }
    public string Status { get; set; }
    public DateTime LastUpdate { get; set; }
    public string Location { get; set; }
}
