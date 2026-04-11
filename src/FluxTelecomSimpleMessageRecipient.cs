using System;
using System.Globalization;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Single recipient entry used by the Flux Telecom simple message form.
    /// </summary>
    public class FluxTelecomSimpleMessageRecipient
    {
        /// <summary>
        /// Two-digit Brazilian area code.
        /// </summary>
        public string AreaCode { get; set; } = default!;

        /// <summary>
        /// Local phone number without the area code.
        /// </summary>
        public string Number { get; set; } = default!;

        /// <summary>
        /// Friendly recipient name embedded in the portal token.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Validates the recipient before it is serialized to the portal token format.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(AreaCode))
                throw new ArgumentException("AreaCode is required.", nameof(AreaCode));

            if (string.IsNullOrWhiteSpace(Number))
                throw new ArgumentException("Number is required.", nameof(Number));

            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("Name is required.", nameof(Name));

            var normalizedAreaCode = NormalizeDigits(AreaCode);
            if (normalizedAreaCode.Length != 2)
                throw new ArgumentException("AreaCode must contain 2 digits after normalization.", nameof(AreaCode));

            var normalizedNumber = NormalizeDigits(Number);
            if (normalizedNumber.Length < 8 || normalizedNumber.Length > 9)
                throw new ArgumentException("Number must contain 8 or 9 digits after normalization.", nameof(Number));
        }

        /// <summary>
        /// Converts the recipient to the portal token format <c>ddd_numero_nome|</c>.
        /// </summary>
        public string ToPortalToken()
        {
            Validate();
            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}|", NormalizeDigits(AreaCode), NormalizeDigits(Number), Name.Trim());
        }

        private static string NormalizeDigits(string value)
            => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
