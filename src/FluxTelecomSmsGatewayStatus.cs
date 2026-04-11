using System;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Operational snapshot used by administrative tools that interact with the Flux Telecom gateway through configured credentials.
    /// </summary>
    public class FluxTelecomSmsGatewayStatus
    {
        /// <summary>
        /// Last credit amount retrieved from the authenticated dashboard.
        /// </summary>
        public int? AvailableCredits { get; set; }

        /// <summary>
        /// Indicates whether configured credentials are currently available.
        /// </summary>
        public bool CredentialsConfigured { get; set; }

        /// <summary>
        /// Callback URL currently configured for the application-level manual send workflow.
        /// </summary>
        public string? CallbackUrl { get; set; }

        /// <summary>
        /// Indicates whether the configured callback URL is usable according to the documented provider constraints.
        /// </summary>
        public bool CallbackUrlValid { get; set; }

        /// <summary>
        /// Last status error captured while querying the gateway, when applicable.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// UTC timestamp of the latest status refresh.
        /// </summary>
        public DateTime CheckedAtUtc { get; set; }
    }
}