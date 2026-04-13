using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Request used by the portal-backed reply list page under <c>respostalista.do</c>.
    /// </summary>
    public class FluxTelecomReplySearchRequest
    {
        /// <summary>
        /// Inclusive start date used by the portal filter.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Inclusive end date used by the portal filter.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Zero-based page index accepted by the AJAX list endpoint.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Optional phone filter accepted by the portal as one or more comma-separated numbers.
        /// </summary>
        public string? PhoneFilter { get; set; }

        /// <summary>
        /// Optional comma-separated cost center identifiers accepted by the portal multi-select.
        /// </summary>
        public string? CostCenterIds { get; set; }

        /// <summary>
        /// Optional comma-separated campaign identifiers accepted by the portal multi-select.
        /// </summary>
        public string? CampaignIds { get; set; }

        /// <summary>
        /// Validates the request according to the portal-backed filter rules already observed in production HTML.
        /// </summary>
        public void Validate()
        {
            if (EndDate.Date < StartDate.Date)
                throw new ArgumentException("EndDate must be greater than or equal to StartDate.", nameof(EndDate));

            if (Page < 0)
                throw new ArgumentOutOfRangeException(nameof(Page), "Page must be greater than or equal to zero.");

            NormalizePhoneTokens(PhoneFilter, validateLength: true);
            NormalizeIdentifierTokens(CostCenterIds, nameof(CostCenterIds));
            NormalizeIdentifierTokens(CampaignIds, nameof(CampaignIds));
        }

        /// <summary>
        /// Formats <see cref="StartDate"/> using the portal date convention.
        /// </summary>
        public string GetStartDateText()
            => StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formats <see cref="EndDate"/> using the portal date convention.
        /// </summary>
        public string GetEndDateText()
            => EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns the normalized phone filter exactly as expected by the AJAX endpoint.
        /// </summary>
        public string GetNormalizedPhoneFilter()
            => string.Join(",", NormalizePhoneTokens(PhoneFilter, validateLength: true));

        /// <summary>
        /// Returns the normalized cost center identifiers exactly as expected by the AJAX endpoint.
        /// </summary>
        public string GetNormalizedCostCenterIds()
            => string.Join(",", NormalizeIdentifierTokens(CostCenterIds, nameof(CostCenterIds)));

        /// <summary>
        /// Returns the normalized campaign identifiers exactly as expected by the AJAX endpoint.
        /// </summary>
        public string GetNormalizedCampaignIds()
            => string.Join(",", NormalizeIdentifierTokens(CampaignIds, nameof(CampaignIds)));

        private static IReadOnlyList<string> NormalizePhoneTokens(string? value, bool validateLength)
        {
            var tokens = new List<string>();

            foreach (var rawToken in SplitCsv(value))
            {
                var digits = ExtractDigits(rawToken);
                if (digits.Length == 0)
                    throw new ArgumentException("PhoneFilter contains an invalid token.", nameof(PhoneFilter));

                if (validateLength && (digits.Length < 8 || digits.Length > 11))
                    throw new ArgumentException("Each phone filter token must contain between 8 and 11 digits after normalization.", nameof(PhoneFilter));

                tokens.Add(digits);
            }

            return tokens;
        }

        private static IReadOnlyList<string> NormalizeIdentifierTokens(string? value, string parameterName)
        {
            var tokens = new List<string>();

            foreach (var rawToken in SplitCsv(value))
            {
                var digits = ExtractDigits(rawToken);
                if (digits.Length == 0)
                    throw new ArgumentException($"{parameterName} contains an invalid token.", parameterName);

                tokens.Add(digits);
            }

            return tokens;
        }

        private static IEnumerable<string> SplitCsv(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;

            foreach (var token in (value ?? string.Empty).Split(','))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    yield return trimmed;
            }
        }

        private static string ExtractDigits(string value)
            => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}