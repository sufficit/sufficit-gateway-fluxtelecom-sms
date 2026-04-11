using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Reads either a JSON string or a JSON number as a string value.
    /// </summary>
    public sealed class FluxTelecomStringOrNumberJsonConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var integerValue))
                        return integerValue.ToString(CultureInfo.InvariantCulture);

                    if (reader.TryGetDecimal(out var decimalValue))
                        return decimalValue.ToString(CultureInfo.InvariantCulture);

                    break;
            }

            throw new JsonException("Expected a string or numeric JSON token.");
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value);
        }
    }
}