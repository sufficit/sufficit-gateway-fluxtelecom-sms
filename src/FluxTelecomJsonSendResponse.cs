using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Minimal typed response for the official Flux Telecom JSON send endpoints.
    /// </summary>
    public class FluxTelecomJsonSendResponse
    {
        /// <summary>
        /// Provider code returned in <c>codigo</c> when the API uses the code-description format.
        /// </summary>
        [JsonPropertyName("codigo")]
        public string? Code { get; set; }

        /// <summary>
        /// Provider description returned in <c>descricao_retorno</c>.
        /// </summary>
        [JsonPropertyName("descricao_retorno")]
        public string? ReturnDescription { get; set; }

        /// <summary>
        /// Provider message identifier returned in <c>id_mensagem</c> when the endpoint answers with JSON,
        /// or the accepted plain-text token echoed by the provider when <c>/envio</c> returns text instead of JSON.
        /// </summary>
        [JsonPropertyName("id_mensagem")]
        [JsonConverter(typeof(FluxTelecomStringOrNumberJsonConverter))]
        public string? MessageId { get; set; }

        /// <summary>
        /// Preserves any extra fields returned by the provider that are not modeled explicitly.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement>? AdditionalData { get; set; }
    }
}