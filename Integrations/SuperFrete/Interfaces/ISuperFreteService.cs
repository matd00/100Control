namespace Integrations.SuperFrete.Interfaces;

public interface ISuperFreteService
{
    Task<decimal> CalculateFreightAsync(FreightQuoteRequest request);
    Task<string> GenerateLabelAsync(ShipmentLabelRequest request);
    Task<ShipmentTrackingDto> TrackShipmentAsync(string trackingNumber);
}

public class FreightQuoteRequest
{
    public string PostalCode { get; set; }
    public decimal Weight { get; set; }
    public int Quantity { get; set; }
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
