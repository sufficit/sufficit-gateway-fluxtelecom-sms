using System;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Summary returned after a portal-backed simple message submission.
    /// </summary>
    public sealed class FluxTelecomSimpleMessageSendResult
    {
        /// <summary>
        /// Relative portal path used by the final response.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Fully resolved URL returned by the last HTTP response.
        /// </summary>
        public string ResolvedUrl { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the final response still looks like the login page.
        /// </summary>
        public bool IsLoginPage { get; set; }

        /// <summary>
        /// Indicates whether the client detected that the portal requires a new login.
        /// </summary>
        public bool RequiresLogin { get; set; }

        /// <summary>
        /// Indicates whether the portal explicitly denied access to the target page.
        /// </summary>
        public bool AccessDenied { get; set; }

        /// <summary>
        /// Indicates whether the portal answered with the provider invalid-route marker.
        /// </summary>
        public bool InvalidUrl { get; set; }

        /// <summary>
        /// Indicates whether the final response still looks like an authenticated portal page.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Optional credit balance observed before the simple send request.
        /// </summary>
        public int? AvailableCreditsBefore { get; set; }

        /// <summary>
        /// Optional credit balance observed after the simple send request.
        /// </summary>
        public int? AvailableCreditsAfter { get; set; }

        /// <summary>
        /// Positive credit delta observed between <see cref="AvailableCreditsBefore"/> and <see cref="AvailableCreditsAfter"/>.
        /// </summary>
        public int? ConsumedCredits { get; set; }

        /// <summary>
        /// Indicates whether the acceptance heuristic was confirmed by a positive credit delta.
        /// </summary>
        public bool AcceptedByCreditDelta { get; set; }

        /// <summary>
        /// Indicates whether the acceptance heuristic was confirmed only by the authenticated portal state.
        /// </summary>
        public bool AcceptedByPortalState { get; set; }

        /// <summary>
        /// Consolidated acceptance heuristic for the portal-backed simple send.
        /// </summary>
        public bool Accepted { get; set; }

        /// <summary>
        /// Creates a summarized result from the raw portal page and the optional credit snapshots.
        /// </summary>
        public static FluxTelecomSimpleMessageSendResult Create(
            FluxTelecomPortalPage page,
            int? availableCreditsBefore,
            int? availableCreditsAfter)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            var consumedCredits = availableCreditsBefore.HasValue && availableCreditsAfter.HasValue
                ? availableCreditsBefore.Value - availableCreditsAfter.Value
                : (int?)null;

            var acceptedByCreditDelta = consumedCredits.HasValue && consumedCredits.Value > 0;
            var acceptedByPortalState = page.IsAuthenticated
                && !page.RequiresLogin
                && !page.AccessDenied
                && !page.InvalidUrl;

            return new FluxTelecomSimpleMessageSendResult()
            {
                RelativePath = page.RelativePath ?? string.Empty,
                ResolvedUrl = page.ResolvedUrl ?? string.Empty,
                IsLoginPage = page.IsLoginPage,
                RequiresLogin = page.RequiresLogin,
                AccessDenied = page.AccessDenied,
                InvalidUrl = page.InvalidUrl,
                IsAuthenticated = page.IsAuthenticated,
                AvailableCreditsBefore = availableCreditsBefore,
                AvailableCreditsAfter = availableCreditsAfter,
                ConsumedCredits = consumedCredits,
                AcceptedByCreditDelta = acceptedByCreditDelta,
                AcceptedByPortalState = acceptedByPortalState,
                Accepted = acceptedByCreditDelta || acceptedByPortalState,
            };
        }
    }
}