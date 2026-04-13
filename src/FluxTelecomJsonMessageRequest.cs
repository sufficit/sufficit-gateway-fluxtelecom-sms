using System;
using System.Text.Json.Serialization;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Single message payload for the official Flux Telecom JSON POST API described in section 7.1 of the provider manual.
    /// </summary>
    public class FluxTelecomJsonMessageRequest
    {
        /// <summary>
        /// Recipient phone number formatted according to the provider examples, such as <c>11988889999</c> or <c>5511988889999</c>.
        /// </summary>
        [JsonPropertyName("to")]
        public string To { get; set; } = default!;

        /// <summary>
        /// Service selector sent in <c>tipoEnvio</c>, for example <c>1</c>, <c>2</c>, <c>4</c>, or <c>7</c>.
        /// </summary>
        [JsonPropertyName("tipoEnvio")]
        public string SendType { get; set; } = default!;

        /// <summary>
        /// SMS body text sent in <c>msg</c>.
        /// </summary>
        [JsonPropertyName("msg")]
        public string Message { get; set; } = default!;

        /// <summary>
        /// Optional origin-system identifier sent in <c>id</c>.
        /// </summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PartnerId { get; set; }

        /// <summary>
        /// Optional cost-center identifier sent in <c>client</c>.
        /// </summary>
        [JsonPropertyName("client")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Client { get; set; }

        /// <summary>
        /// Optional recipient name sent in <c>form</c>.
        /// </summary>
        [JsonPropertyName("form")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RecipientName { get; set; }

        /// <summary>
        /// Optional shortcode identifier sent in <c>idShort</c>.
        /// </summary>
        [JsonPropertyName("idShort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ShortCodeId { get; set; }

        /// <summary>
        /// Optional DDI value sent in <c>ddi</c>.
        /// </summary>
        [JsonPropertyName("ddi")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Ddi { get; set; }

        /// <summary>
        /// Optional callback URL sent in <c>urlCallback</c>.
        /// </summary>
        [JsonPropertyName("urlCallback")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CallbackUrl { get; set; }

        /// <summary>
        /// Optional callback correlation token sent in <c>tokenCallback</c>.
        /// </summary>
        [JsonPropertyName("tokenCallback")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CallbackToken { get; set; }

        /// <summary>
        /// Optional managed link target sent in <c>linkUrl</c>.
        /// </summary>
        [JsonPropertyName("linkUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LinkUrl { get; set; }

        /// <summary>
        /// Optional WhatsApp introductory text sent in <c>whatsText</c>.
        /// </summary>
        [JsonPropertyName("whatsText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WhatsText { get; set; }

        /// <summary>
        /// Optional WhatsApp phone number sent in <c>whatsNum</c>.
        /// </summary>
        [JsonPropertyName("whatsNum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WhatsNumber { get; set; }

        /// <summary>
        /// Optional scheduling value sent in the provider textual format <c>dd/MM/yyyy HH:mm:ss</c>.
        /// </summary>
        [JsonPropertyName("schedule")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Schedule { get; set; }

        [JsonPropertyName("coluna_a")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnA { get; set; }

        [JsonPropertyName("coluna_b")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnB { get; set; }

        [JsonPropertyName("coluna_c")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnC { get; set; }

        [JsonPropertyName("coluna_d")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnD { get; set; }

        [JsonPropertyName("coluna_e")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnE { get; set; }

        [JsonPropertyName("coluna_f")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnF { get; set; }

        [JsonPropertyName("coluna_g")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnG { get; set; }

        [JsonPropertyName("coluna_h")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnH { get; set; }

        [JsonPropertyName("coluna_i")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnI { get; set; }

        [JsonPropertyName("coluna_j")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnJ { get; set; }

        /// <summary>
        /// Validates the request before it is serialized to the provider JSON API.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(To))
                throw new ArgumentException("To is required.", nameof(To));

            if (string.IsNullOrWhiteSpace(SendType))
                throw new ArgumentException("SendType is required.", nameof(SendType));

            if (string.IsNullOrWhiteSpace(Message))
                throw new ArgumentException("Message is required.", nameof(Message));

            if (!string.IsNullOrWhiteSpace(CallbackUrl))
            {
                if (!Uri.IsWellFormedUriString(CallbackUrl, UriKind.Absolute))
                    throw new ArgumentException("CallbackUrl must be a valid absolute URL.", nameof(CallbackUrl));
            }
        }
    }
}