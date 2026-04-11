using System.Collections.Generic;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Basic dashboard information extracted from the authenticated portal home.
    /// </summary>
    public class FluxTelecomDashboard
    {
        /// <summary>
        /// Display name shown by the authenticated portal header.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Available SMS credits parsed from the header widget.
        /// </summary>
        public int? AvailableCredits { get; set; }

        /// <summary>
        /// Distinct menu labels discovered in the authenticated navigation area.
        /// </summary>
        public IReadOnlyList<string> MenuItems { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Raw HTML used to build the parsed dashboard result.
        /// </summary>
        public string Html { get; set; } = string.Empty;
    }
}
