using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBank.Convertion;

public class AmountJsonConverter : JsonConverter<Amount>
{
    public override Amount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long value = reader.GetInt64();
        return new Amount(value);
    }

    public override void Write(Utf8JsonWriter writer, Amount value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}