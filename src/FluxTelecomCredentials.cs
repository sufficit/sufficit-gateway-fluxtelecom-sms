namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Login credentials used by the Flux Telecom portal session and by the official account/code API surface.
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

        /// <summary>
        /// Resolves the effective account header used by the official JSON API.
        /// </summary>
        /// <returns>The trimmed account value when available; otherwise <see langword="null"/>.</returns>
        public string? GetResolvedAccount()
            => NormalizeOptionalText(Email);

        /// <summary>
        /// Resolves the effective code header used by the official JSON API.
        /// </summary>
        /// <returns>The trimmed code value when available; otherwise <see langword="null"/>.</returns>
        public string? GetResolvedCode()
            => NormalizeOptionalText(Password);

        private static string? NormalizeOptionalText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
