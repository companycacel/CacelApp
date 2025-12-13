using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Shared.Validators;

public class ToStringNullableConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();

        try
        {
            // Para otros tipos (números, booleanos, etc.), usar JsonDocument para obtener el valor como string
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                return doc.RootElement.ToString();
            }
        }
        catch
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}