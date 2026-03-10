using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.SuperFrete.Models;

#region JSON Converters

/// <summary>
/// Converter que aceita string, número ou null e converte para string
/// </summary>
public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long longValue))
                    return longValue.ToString();
                if (reader.TryGetDecimal(out decimal decimalValue))
                    return decimalValue.ToString();
                return reader.GetDouble().ToString();
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.Null:
                return null;
            default:
                // Para objetos ou arrays, retorna null
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}

/// <summary>
/// Converter que aceita int ou string e converte para int
/// </summary>
public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.String:
                var str = reader.GetString();
                if (int.TryParse(str, out int result))
                    return result;
                return 0;
            default:
                reader.Skip();
                return 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// Converter que aceita decimal ou string e converte para decimal
/// </summary>
public class FlexibleDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDecimal();
            case JsonTokenType.String:
                var str = reader.GetString();
                if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                    return result;
                return 0;
            default:
                reader.Skip();
                return 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

#endregion

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

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("complement")]
    public string? Complement { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state_abbr")]
    public string? StateAbbr { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
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

public class SuperFreteVolume
{
    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; } = 11;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 2;

    [JsonPropertyName("length")]
    public int Length { get; set; } = 16;
}

public class SuperFreteProduct
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("unitary_value")]
    public decimal UnitaryValue { get; set; }
}

public class SuperFreteShipmentRequest
{
    [JsonPropertyName("from")]
    public SuperFreteAddress From { get; set; } = new();

    [JsonPropertyName("to")]
    public SuperFreteAddress To { get; set; } = new();

    [JsonPropertyName("products")]
    public List<SuperFreteProduct> Products { get; set; } = new();

    [JsonPropertyName("volumes")]
    public List<SuperFreteVolume> Volumes { get; set; } = new();

    [JsonPropertyName("options")]
    public SuperFreteOptions? Options { get; set; }

    [JsonPropertyName("service")]
    public int Service { get; set; }

    [JsonPropertyName("invoice")]
    public SuperFreteInvoice? Invoice { get; set; }
}

public class SuperFreteOptions
{
    [JsonPropertyName("insurance_value")]
    public decimal InsuranceValue { get; set; }

    [JsonPropertyName("receipt")]
    public bool Receipt { get; set; }

    [JsonPropertyName("own_hand")]
    public bool OwnHand { get; set; }

    [JsonPropertyName("non_commercial")]
    public bool NonCommercial { get; set; } = true;
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
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Id { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal Price { get; set; }

    [JsonPropertyName("discount")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal Discount { get; set; }

    [JsonPropertyName("delivery_time")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DeliveryTime { get; set; }

    [JsonPropertyName("delivery_range")]
    public SuperFreteDeliveryRange? DeliveryRange { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("company")]
    public SuperFreteCompany? Company { get; set; }

    [JsonPropertyName("error")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Error { get; set; }
}

public class SuperFreteDeliveryRange
{
    [JsonPropertyName("min")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Max { get; set; }
}

public class SuperFreteCompany
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("picture")]
    [JsonConverter(typeof(FlexibleStringConverter))]
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
