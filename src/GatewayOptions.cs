using System;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Configuration options for Flux Telecom SMS portal access.
    /// </summary>
    public class GatewayOptions
    {
        /// <summary>
        /// Canonical configuration section used to bind gateway options.
        /// </summary>
        public const string SECTIONNAME = "Sufficit:Gateway:FluxTelecom:SMS";

        /// <summary>
        /// Absolute base URL used by portal-backed requests.
        /// </summary>
        public string BaseUrl { get; set; } = "https://sms.fluxtelecom.com.br/";

        /// <summary>
        /// User-Agent header sent in outbound HTTP requests.
        /// </summary>
        public string Agent { get; set; } = "Sufficit Flux Telecom SMS Gateway";

        /// <summary>
        /// Allows bypassing certificate validation when the remote portal exposes an invalid TLS chain.
        /// </summary>
        public bool AllowInvalidServerCertificate { get; set; } = true;

        /// <summary>
        /// Default timeout in seconds for portal requests.
        /// </summary>
        public uint? TimeoutSeconds { get; set; } = 30;
    }
}
