using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Single message status row returned by the official Flux Telecom JSON consultation endpoint.
    /// </summary>
    public class FluxTelecomMessageStatusEntry
    {
        /// <summary>
        /// Provider-generated message identifier returned in <c>id_mensagem</c>.
        /// </summary>
        [JsonPropertyName("id_mensagem")]
        public long? MessageId { get; set; }

        /// <summary>
        /// Raw provider delivery timestamp returned in <c>data_entrega</c>.
        /// </summary>
        [JsonPropertyName("data_entrega")]
        public string? DeliveredAtText { get; set; }

        /// <summary>
        /// Numeric provider status identifier returned in <c>id_status</c>.
        /// </summary>
        [JsonPropertyName("id_status")]
        public int? StatusId { get; set; }

        /// <summary>
        /// Raw inclusion timestamp returned in <c>data_inclusao</c>.
        /// </summary>
        [JsonPropertyName("data_inclusao")]
        public string? IncludedAtText { get; set; }

        /// <summary>
        /// Provider customer identifier returned in <c>id_parceiro</c>.
        /// </summary>
        [JsonPropertyName("id_parceiro")]
        public string? PartnerId { get; set; }

        /// <summary>
        /// Raw provider send timestamp returned in <c>data_envio</c>.
        /// </summary>
        [JsonPropertyName("data_envio")]
        public string? SentAtText { get; set; }

        /// <summary>
        /// Human-readable provider status description returned in <c>descricao_status</c>.
        /// </summary>
        [JsonPropertyName("descricao_status")]
        public string? StatusDescription { get; set; }
    }
}