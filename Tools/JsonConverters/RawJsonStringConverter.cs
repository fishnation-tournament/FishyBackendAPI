using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishyAPI.Tools;

// Writes a JSON string column as raw JSON in API responses.
public sealed class RawJsonStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();

        // Client sent a raw JSON object/array (e.g. round-tripping a FrontFacingUser response)
        using var doc = JsonDocument.ParseValue(ref reader);
        return doc.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            writer.WriteNullValue();
            return;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(value);
            doc.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteNullValue();
        }
    }
}

