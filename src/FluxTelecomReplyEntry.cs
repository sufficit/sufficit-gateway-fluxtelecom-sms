namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Single reply entry returned by the portal-backed reply list page.
    /// </summary>
    public class FluxTelecomReplyEntry
    {
        /// <summary>
        /// Internal provider reply identifier extracted from the portal detail action.
        /// </summary>
        public int? ReplyId { get; set; }

        /// <summary>
        /// Provider campaign identifier associated with the original send.
        /// </summary>
        public int? CampaignId { get; set; }

        /// <summary>
        /// Provider message identifier associated with the original send.
        /// </summary>
        public long? MessageId { get; set; }

        /// <summary>
        /// Campaign description rendered by the portal list.
        /// </summary>
        public string CampaignName { get; set; } = string.Empty;

        /// <summary>
        /// Cost center description rendered by the portal list.
        /// </summary>
        public string CostCenterDescription { get; set; } = string.Empty;

        /// <summary>
        /// Original outbound message text rendered by the portal list.
        /// </summary>
        public string SentMessage { get; set; } = string.Empty;

        /// <summary>
        /// Recipient reply text rendered by the portal list.
        /// </summary>
        public string ReplyText { get; set; } = string.Empty;

        /// <summary>
        /// Sender phone rendered by the portal list.
        /// </summary>
        public string SenderPhone { get; set; } = string.Empty;

        /// <summary>
        /// Optional sender contact hint rendered in the phone link title attribute.
        /// </summary>
        public string? SenderContactHint { get; set; }

        /// <summary>
        /// Portal-local timestamp text rendered by the list.
        /// </summary>
        public string ReceivedAtText { get; set; } = string.Empty;
    }
}