using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Request payload for the portal page "Mensagem Simples".
    /// </summary>
    public class FluxTelecomSimpleMessageRequest
    {
        private const string DEFAULT_SERVICE_OPTION = "7103;2;160";
        private const string DEFAULT_GROUP_MODEL_ID = "0";
        private const string DEFAULT_MODEL_ID = "@indTipo#1";

        /// <summary>
        /// Portal service selector in the format <c>&lt;serviceId&gt;;&lt;smsType&gt;;&lt;charLimit&gt;</c>.
        /// </summary>
        public string ServiceOption { get; set; } = DEFAULT_SERVICE_OPTION;

        /// <summary>
        /// Optional cost center identifier.
        /// </summary>
        public int? CostCenterId { get; set; }

        /// <summary>
        /// When true, sends immediately; otherwise the portal scheduling fields are used.
        /// </summary>
        public bool ScheduleImmediately { get; set; } = true;

        /// <summary>
        /// Scheduled send date used when <see cref="ScheduleImmediately"/> is false.
        /// </summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>
        /// Group model identifier posted by the portal form.
        /// </summary>
        public string GroupModelId { get; set; } = DEFAULT_GROUP_MODEL_ID;

        /// <summary>
        /// Message model identifier posted by the portal form.
        /// </summary>
        public string ModelId { get; set; } = DEFAULT_MODEL_ID;

        /// <summary>
        /// Enables the managed link option in the portal payload.
        /// </summary>
        public bool UseManagedLink { get; set; }

        /// <summary>
        /// Managed link value sent when <see cref="UseManagedLink"/> is enabled.
        /// </summary>
        public string ManagedLink { get; set; } = string.Empty;

        /// <summary>
        /// Enables the WhatsApp companion link option in the portal payload.
        /// </summary>
        public bool UseWhatsAppLink { get; set; }

        /// <summary>
        /// WhatsApp phone number posted when the WhatsApp option is enabled.
        /// </summary>
        public string WhatsAppPhone { get; set; } = string.Empty;

        /// <summary>
        /// Introductory WhatsApp text posted when the WhatsApp option is enabled.
        /// </summary>
        public string WhatsAppText { get; set; } = string.Empty;

        /// <summary>
        /// SMS body text sent by the portal.
        /// </summary>
        public string Message { get; set; } = default!;

        /// <summary>
        /// One or more recipients serialized to the portal list token.
        /// </summary>
        public IList<FluxTelecomSimpleMessageRecipient> Recipients { get; set; } = new List<FluxTelecomSimpleMessageRecipient>();

        /// <summary>
        /// Validates the request before it is serialized to the portal form.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServiceOption))
                throw new ArgumentException("ServiceOption is required.", nameof(ServiceOption));

            if (string.IsNullOrWhiteSpace(Message))
                throw new ArgumentException("Message is required.", nameof(Message));

            if (Recipients == null || Recipients.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(Recipients));

            foreach (var recipient in Recipients)
                recipient.Validate();

            if (!ScheduleImmediately && !ScheduledAt.HasValue)
                throw new ArgumentException("ScheduledAt must be provided when ScheduleImmediately is false.", nameof(ScheduledAt));

            if (UseManagedLink && string.IsNullOrWhiteSpace(ManagedLink))
                throw new ArgumentException("ManagedLink must be provided when UseManagedLink is true.", nameof(ManagedLink));
        }

        /// <summary>
        /// Concatenates all recipients into the portal token list expected by <c>campanhaMensagemVo.listaNumero</c>.
        /// </summary>
        public string GetRecipientListToken()
        {
            Validate();
            return string.Concat(Recipients.Select(recipient => recipient.ToPortalToken()));
        }

        /// <summary>
        /// Returns the portal scheduling flag for the current request.
        /// </summary>
        public string GetScheduledFlag()
            => ScheduleImmediately ? "1" : "2";

        /// <summary>
        /// Formats the scheduled date using the portal date-time convention.
        /// </summary>
        public string GetScheduledAtText()
            => ScheduledAt.HasValue
                ? ScheduledAt.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                : string.Empty;

        /// <summary>
        /// Returns the cost center identifier as text or an empty string when omitted.
        /// </summary>
        public string GetCostCenterText()
            => CostCenterId.HasValue
                ? CostCenterId.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;

        /// <summary>
        /// Returns the portal flag for the managed link option.
        /// </summary>
        public string GetManagedLinkFlag()
            => UseManagedLink ? "1" : "2";

        /// <summary>
        /// Returns the portal flag for the WhatsApp companion link option.
        /// </summary>
        public string GetWhatsAppFlag()
            => UseWhatsAppLink ? "1" : "2";

        /// <summary>
        /// Returns the service identifier extracted from <see cref="ServiceOption"/>.
        /// </summary>
        public string GetServiceIdText()
            => SplitServiceOption()[0];

        /// <summary>
        /// Returns the SMS type identifier extracted from <see cref="ServiceOption"/>.
        /// </summary>
        public string GetSmsTypeText()
            => SplitServiceOption()[1];

        private string[] SplitServiceOption()
        {
            var parts = (ServiceOption ?? string.Empty).Split(';');
            if (parts.Length < 3)
                throw new ArgumentException("ServiceOption must contain the format '<serviceId>;<smsType>;<charLimit>'.", nameof(ServiceOption));

            return parts;
        }
    }
}
