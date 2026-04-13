using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Root response returned by the official Flux Telecom JSON message status consultation endpoint.
    /// </summary>
    public class FluxTelecomMessageStatusResponse
    {
        /// <summary>
        /// Optional provider code returned when the endpoint answers with a code-description payload instead of the message list.
        /// </summary>
        [JsonPropertyName("codigo")]
        public string? Code { get; set; }

        /// <summary>
        /// Optional provider description returned together with <see cref="Code"/>.
        /// </summary>
        [JsonPropertyName("descricao_retorno")]
        public string? ReturnDescription { get; set; }

        /// <summary>
        /// Collection returned in the provider field <c>mensagens</c>.
        /// </summary>
        [JsonPropertyName("mensagens")]
        public IList<FluxTelecomMessageStatusEntry> Messages { get; set; } = new List<FluxTelecomMessageStatusEntry>();
    }
}