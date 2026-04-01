using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishyAPI.Tools;

public class UInt64ToStringConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();
            if (ulong.TryParse(stringValue, out ulong value))
            {
                return value;
            }

            throw new JsonException($"Invalid unsigned integer string '{stringValue}'.");
        }

        // Force string-only for large IDs so client-side number rounding cannot corrupt values.
        throw new JsonException("Expected a JSON string for ulong value.");
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}