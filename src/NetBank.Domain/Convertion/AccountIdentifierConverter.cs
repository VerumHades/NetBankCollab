using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBank.Convertion;


public class AccountIdentifierConverter : JsonConverter<AccountIdentifier>
{
    public override AccountIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int number = reader.GetInt32();
        return new AccountIdentifier(number);
    }

    public override void Write(Utf8JsonWriter writer, AccountIdentifier value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Number);
    }
}
