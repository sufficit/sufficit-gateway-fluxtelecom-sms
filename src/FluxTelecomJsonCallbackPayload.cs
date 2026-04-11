using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Root callback payload delivered by the official Flux Telecom JSON callback workflow.
    /// </summary>
    public class FluxTelecomJsonCallbackPayload
    {
        /// <summary>
        /// Callback items returned in the provider field <c>mensagens</c>.
        /// </summary>
        [JsonPropertyName("mensagens")]
        public IList<FluxTelecomJsonCallbackEntry> Messages { get; set; } = new List<FluxTelecomJsonCallbackEntry>();
    }
}