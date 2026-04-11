using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Request payload for the official Flux Telecom JSON message status consultation described in section 1.7 of the provider manual.
    /// </summary>
    public class FluxTelecomMessageStatusQueryRequest
    {
        /// <summary>
        /// API login sent in the <c>account</c> query-string field.
        /// </summary>
        public string Account { get; set; } = default!;

        /// <summary>
        /// API password sent in the <c>code</c> query-string field.
        /// </summary>
        public string Code { get; set; } = default!;

        /// <summary>
        /// One or more provider-generated message identifiers joined as <c>id=1;2;3</c>.
        /// </summary>
        public IList<long> MessageIds { get; set; } = new List<long>();

        /// <summary>
        /// Validates the request before it is serialized to the JSON consultation URL.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Account))
                throw new ArgumentException("Account is required.", nameof(Account));

            if (string.IsNullOrWhiteSpace(Code))
                throw new ArgumentException("Code is required.", nameof(Code));

            if (MessageIds == null || MessageIds.Count == 0)
                throw new ArgumentException("At least one message id is required.", nameof(MessageIds));

            if (MessageIds.Any(id => id <= 0))
                throw new ArgumentException("MessageIds must contain only values greater than zero.", nameof(MessageIds));
        }

        /// <summary>
        /// Returns the provider id list joined with semicolons as documented by the official manual.
        /// </summary>
        public string GetMessageIdsText()
        {
            Validate();
            return string.Join(";", MessageIds.Select(id => id.ToString(CultureInfo.InvariantCulture)));
        }
    }
}