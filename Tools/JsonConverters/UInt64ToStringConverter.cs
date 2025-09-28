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
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt64(out ulong value))
            {
                return value;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to ulong.");
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}