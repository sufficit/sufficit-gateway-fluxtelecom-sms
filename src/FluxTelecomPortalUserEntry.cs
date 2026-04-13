namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Summary row returned by the Flux Telecom portal user list.
    /// </summary>
    public class FluxTelecomPortalUserEntry
    {
        /// <summary>
        /// Portal user identifier used by the edit form.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Display name shown in the portal list.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User e-mail shown in the portal list.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the portal marks the user as active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}