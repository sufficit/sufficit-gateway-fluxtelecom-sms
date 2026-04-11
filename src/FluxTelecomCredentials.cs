namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Login credentials used by the Flux Telecom portal session.
    /// </summary>
    public class FluxTelecomCredentials
    {
        /// <summary>
        /// Canonical configuration section used to bind portal credentials.
        /// </summary>
        public const string SECTIONNAME = "Sufficit:Gateway:FluxTelecom:Credentials";

        /// <summary>
        /// Portal account login, usually the account e-mail.
        /// </summary>
        public string Email { get; set; } = default!;

        /// <summary>
        /// Portal account password.
        /// </summary>
        public string Password { get; set; } = default!;
    }
}
