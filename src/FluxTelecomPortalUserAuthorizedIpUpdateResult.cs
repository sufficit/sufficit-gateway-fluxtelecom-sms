using System;
using System.Collections.Generic;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Result returned after reconciling the authorized IP list of an existing Flux Telecom portal user.
    /// </summary>
    public class FluxTelecomPortalUserAuthorizedIpUpdateResult
    {
        /// <summary>
        /// Portal user identifier that was resolved for the operation.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User name resolved from the portal form.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User e-mail resolved from the portal form or list.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the client needed to submit an update to the portal.
        /// </summary>
        public bool WasUpdated { get; set; }

        /// <summary>
        /// Authorized IPs found in the portal before the reconciliation step.
        /// </summary>
        public IReadOnlyList<string> OriginalAuthorizedIps { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Authorized IPs kept after the reconciliation step.
        /// </summary>
        public IReadOnlyList<string> AuthorizedIps { get; set; } = Array.Empty<string>();
    }
}