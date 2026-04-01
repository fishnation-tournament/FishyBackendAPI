using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishyAPI.Tools;

public class NullableUInt64ToStringConverter : JsonConverter<ulong?>
{
    public override ulong? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            
            if (ulong.TryParse(stringValue, out ulong value))
            {
                return value;
            }

            throw new JsonException($"Invalid unsigned integer string '{stringValue}'.");
        }

        // Large external IDs (Steam/BeatLeader/Discord) must be sent as strings to avoid JS precision loss.
        throw new JsonException("Expected a JSON string for ulong? value.");
    }

    public override void Write(Utf8JsonWriter writer, ulong? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}