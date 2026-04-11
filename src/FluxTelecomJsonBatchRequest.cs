using System;
using System.Collections.Generic;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Batch payload for the official Flux Telecom JSON POST API described in section 7.2 of the provider manual.
    /// </summary>
    public class FluxTelecomJsonBatchRequest
    {
        private const int MAX_BATCH_MESSAGES = 1000;

        /// <summary>
        /// Batch message list serialized in the provider field <c>mensagens</c>.
        /// </summary>
        public IList<FluxTelecomJsonMessageRequest> Messages { get; set; } = new List<FluxTelecomJsonMessageRequest>();

        /// <summary>
        /// Validates the batch before it is serialized to the provider JSON API.
        /// </summary>
        public void Validate()
        {
            if (Messages == null || Messages.Count == 0)
                throw new ArgumentException("At least one JSON message is required.", nameof(Messages));

            if (Messages.Count > MAX_BATCH_MESSAGES)
                throw new ArgumentException("The provider documents a limit of about 1000 messages per grouped request.", nameof(Messages));

            foreach (var message in Messages)
            {
                if (message == null)
                    throw new ArgumentException("Batch messages cannot contain null items.", nameof(Messages));

                message.Validate();
            }
        }
    }
}