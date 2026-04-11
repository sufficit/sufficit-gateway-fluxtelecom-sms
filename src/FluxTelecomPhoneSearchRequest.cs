using System;
using System.Globalization;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Request used by the phone search page and its report export.
    /// </summary>
    public class FluxTelecomPhoneSearchRequest
    {
        private const int MIN_PHONE_LENGTH = 10;
        private const int MAX_PHONE_LENGTH = 11;
        private const int MAX_RANGE_DAYS = 15;

        /// <summary>
        /// Inclusive start date used by the portal filter.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Inclusive end date used by the portal filter.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Optional cost center identifier used by both the AJAX page and the report export.
        /// </summary>
        public int CostCenterId { get; set; } = -1;

        /// <summary>
        /// Phone number to search, accepted in formatted or digits-only form.
        /// </summary>
        public string Phone { get; set; } = default!;

        /// <summary>
        /// Validates the request against the portal input restrictions.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Phone))
                throw new ArgumentException("Phone is required.", nameof(Phone));

            if (EndDate.Date < StartDate.Date)
                throw new ArgumentException("EndDate must be greater than or equal to StartDate.", nameof(EndDate));

            if ((EndDate.Date - StartDate.Date).TotalDays > MAX_RANGE_DAYS)
                throw new ArgumentOutOfRangeException(nameof(EndDate), $"The Flux Telecom portal only accepts a date range up to {MAX_RANGE_DAYS} days.");

            var normalized = GetNormalizedPhone();
            if (normalized.Length != MIN_PHONE_LENGTH && normalized.Length != MAX_PHONE_LENGTH)
                throw new ArgumentException("Phone must contain 10 or 11 digits after normalization.", nameof(Phone));
        }

        /// <summary>
        /// Returns the phone digits exactly as expected by the portal request fields.
        /// </summary>
        public string GetNormalizedPhone()
            => new string((Phone ?? string.Empty).Where(char.IsDigit).ToArray());

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
    }
}
