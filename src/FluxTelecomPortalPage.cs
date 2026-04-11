namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Raw page result returned by the Flux Telecom portal.
    /// </summary>
    public class FluxTelecomPortalPage
    {
        /// <summary>
        /// Relative path requested by the client.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Final resolved URL after redirects handled by <see cref="System.Net.Http.HttpClient"/>.
        /// </summary>
        public string ResolvedUrl { get; set; } = string.Empty;

        /// <summary>
        /// Raw HTML returned by the portal.
        /// </summary>
        public string Html { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the returned content matches the portal login page markers.
        /// </summary>
        public bool IsLoginPage { get; set; }

        /// <summary>
        /// Indicates whether the caller must re-authenticate before retrying the request.
        /// </summary>
        public bool RequiresLogin { get; set; }

        /// <summary>
        /// Indicates whether the page contains an access denied marker.
        /// </summary>
        public bool AccessDenied { get; set; }

        /// <summary>
        /// Indicates whether the page contains the portal invalid URL marker.
        /// </summary>
        public bool InvalidUrl { get; set; }

        /// <summary>
        /// Indicates whether authenticated header markers were detected in the response HTML.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Builds a classified portal page result from the raw HTTP response data.
        /// </summary>
        /// <param name="relativePath">Requested relative path.</param>
        /// <param name="resolvedUrl">Final response URL after redirects.</param>
        /// <param name="html">Raw HTML body returned by the portal.</param>
        /// <returns>A classified page result with login and access markers precomputed.</returns>
        public static FluxTelecomPortalPage Create(string relativePath, string? resolvedUrl, string html)
        {
            var isAuthenticated = FluxTelecomHtml.ContainsAuthenticatedMarker(html);
            var isLoginPage = FluxTelecomHtml.IsLoginPage(html);
            var resolvedLogin = (resolvedUrl ?? string.Empty).IndexOf("login.do", System.StringComparison.OrdinalIgnoreCase) >= 0;

            return new FluxTelecomPortalPage()
            {
                RelativePath = relativePath ?? string.Empty,
                ResolvedUrl = resolvedUrl ?? string.Empty,
                Html = html ?? string.Empty,
                IsLoginPage = isLoginPage,
                RequiresLogin = isLoginPage || (resolvedLogin && !isAuthenticated),
                AccessDenied = FluxTelecomHtml.ContainsAccessDenied(html),
                InvalidUrl = FluxTelecomHtml.ContainsInvalidUrl(html),
                IsAuthenticated = isAuthenticated
            };
        }
    }
}
