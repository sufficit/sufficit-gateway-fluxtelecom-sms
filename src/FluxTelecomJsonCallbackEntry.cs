using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Single callback item delivered by the official Flux Telecom JSON callback workflow.
    /// </summary>
    public class FluxTelecomJsonCallbackEntry
    {
        /// <summary>
        /// Recipient phone number returned by the provider callback.
        /// </summary>
        [JsonPropertyName("telefone")]
        public string? Phone { get; set; }

        /// <summary>
        /// Raw provider timestamp returned in the callback payload.
        /// </summary>
        [JsonPropertyName("data")]
        public string? DateText { get; set; }

        /// <summary>
        /// Provider-generated message identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public long? MessageId { get; set; }

        /// <summary>
        /// Origin-system identifier returned by the provider callback.
        /// </summary>
        [JsonPropertyName("idParceiro")]
        public string? PartnerId { get; set; }

        /// <summary>
        /// Provider callback status such as <c>ENTREGUE</c> or <c>RESPOSTA</c>.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Optional recipient reply text when the callback represents an MO response.
        /// </summary>
        [JsonPropertyName("resposta")]
        public string? ResponseText { get; set; }
    }
}