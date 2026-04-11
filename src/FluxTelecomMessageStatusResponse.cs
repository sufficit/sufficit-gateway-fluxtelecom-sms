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
        /// Collection returned in the provider field <c>mensagens</c>.
        /// </summary>
        [JsonPropertyName("mensagens")]
        public IList<FluxTelecomMessageStatusEntry> Messages { get; set; } = new List<FluxTelecomMessageStatusEntry>();
    }
}