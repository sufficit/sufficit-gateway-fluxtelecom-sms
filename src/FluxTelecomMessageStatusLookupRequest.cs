using System;
using System.Collections.Generic;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Simplified message-status lookup payload used by higher application layers that rely on configured credentials.
    /// </summary>
    public class FluxTelecomMessageStatusLookupRequest
    {
        /// <summary>
        /// One or more provider-generated message identifiers.
        /// </summary>
        public IList<long> MessageIds { get; set; } = new List<long>();

        /// <summary>
        /// Validates the lookup payload before it is converted to the provider-specific query request.
        /// </summary>
        public void Validate()
        {
            if (MessageIds == null || MessageIds.Count == 0)
                throw new ArgumentException("At least one message id is required.", nameof(MessageIds));

            if (MessageIds.Any(id => id <= 0))
                throw new ArgumentException("MessageIds must contain only values greater than zero.", nameof(MessageIds));
        }
    }
}