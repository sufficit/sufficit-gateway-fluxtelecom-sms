using System;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Represents an authentication failure while opening a Flux Telecom portal session.
    /// </summary>
    public class FluxTelecomAuthenticationException : Exception
    {
        /// <summary>
        /// Initializes a new authentication exception with a plain message.
        /// </summary>
        public FluxTelecomAuthenticationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new authentication exception with a plain message and nested exception.
        /// </summary>
        public FluxTelecomAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
