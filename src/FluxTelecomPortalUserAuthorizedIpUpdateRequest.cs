using System;
using System.Collections.Generic;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Portal-backed request used to ensure one or more authorized IPs in an existing Flux Telecom user.
    /// </summary>
    public class FluxTelecomPortalUserAuthorizedIpUpdateRequest
    {
        /// <summary>
        /// Optional portal user identifier. When omitted, <see cref="Email"/> is used to resolve the user from the portal list.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Optional user e-mail used to resolve the portal user when <see cref="UserId"/> is not provided.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Replaces the current portal allowlist when <see langword="true"/>. When <see langword="false"/>, the new IPs are merged with the current list.
        /// </summary>
        public bool ReplaceExisting { get; set; }

        /// <summary>
        /// Authorized IPv4 entries or ranges to keep in the portal field.
        /// </summary>
        public ICollection<string> AuthorizedIps { get; } = new List<string>();

        /// <summary>
        /// Validates the update request before the client issues portal requests.
        /// </summary>
        public void Validate()
        {
            if ((!UserId.HasValue || UserId.Value <= 0) && string.IsNullOrWhiteSpace(Email))
                throw new ArgumentException("Either UserId or Email must be informed to update Flux Telecom authorized IPs.");

            if (NormalizeAuthorizedIpTokens(AuthorizedIps).Count == 0)
                throw new ArgumentException("At least one authorized IP entry must be informed.", nameof(AuthorizedIps));
        }

        internal IReadOnlyList<string> GetNormalizedAuthorizedIps()
            => NormalizeAuthorizedIpTokens(AuthorizedIps);

        internal static IReadOnlyList<string> NormalizeAuthorizedIpTokens(IEnumerable<string> values)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var parts = value.Split(new[] { ',', ';', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var normalized = part.Trim();
                    if (normalized.Length == 0)
                        continue;

                    if (seen.Add(normalized))
                        result.Add(normalized);
                }
            }

            return result;
        }
    }
}