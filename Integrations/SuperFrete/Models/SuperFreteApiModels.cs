using System.Text.Json.Serialization;

namespace Integrations.SuperFrete.Models;

#region Request Models

public class SuperFreteQuoteRequest
{
    [JsonPropertyName("from")]
    public SuperFreteAddress From { get; set; } = new();

    [JsonPropertyName("to")]
    public SuperFreteAddress To { get; set; } = new();

    [JsonPropertyName("package")]
    public SuperFretePackage Package { get; set; } = new();

    [JsonPropertyName("services")]
    public string? Services { get; set; }
}

public class SuperFreteAddress
{
    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state_abbr")]
    public string? StateAbbr { get; set; }
}

public class SuperFretePackage
{
    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; } = 11; // Mínimo Correios

    [JsonPropertyName("height")]
    public int Height { get; set; } = 2; // Mínimo Correios

    [JsonPropertyName("length")]
    public int Length { get; set; } = 16; // Mínimo Correios
}

public class SuperFreteShipmentRequest
{
    [JsonPropertyName("from")]
    public SuperFreteAddress From { get; set; } = new();

    [JsonPropertyName("to")]
    public SuperFreteAddress To { get; set; } = new();

    [JsonPropertyName("package")]
    public SuperFretePackage Package { get; set; } = new();

    [JsonPropertyName("service")]
    public int Service { get; set; }

    [JsonPropertyName("invoice")]
    public SuperFreteInvoice? Invoice { get; set; }
}

public class SuperFreteInvoice
{
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }
}

#endregion

#region Response Models

public class SuperFreteQuoteResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    [JsonPropertyName("delivery_time")]
    public int DeliveryTime { get; set; }

    [JsonPropertyName("delivery_range")]
    public SuperFreteDeliveryRange? DeliveryRange { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("company")]
    public SuperFreteCompany? Company { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class SuperFreteDeliveryRange
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }
}

public class SuperFreteCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

public class SuperFreteShipmentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("tracking")]
    public string? Tracking { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("print")]
    public SuperFretePrint? Print { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class SuperFretePrint
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class SuperFreteTrackingResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("tracking")]
    public string? Tracking { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("events")]
    public List<SuperFreteTrackingEvent>? Events { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class SuperFreteTrackingEvent
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

#endregion
