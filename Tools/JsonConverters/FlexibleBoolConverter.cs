using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishyAPI.Tools;

/// <summary>Reads true/false, or 0/1 from older clients that sent flags as numbers. Always writes true/false.</summary>
public class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number when reader.TryGetInt64(out long number) && (number == 0 || number == 1):
                return number == 1;
            default:
                throw new JsonException("Expected true/false (or 0/1) for a boolean value.");
        }
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
