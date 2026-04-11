using System;
using System.Net.Http;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    internal static class HttpExtensions
    {
        public static HttpClient Configure(this HttpClient source, GatewayOptions options)
        {
            source.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);

            if (options.TimeoutSeconds.HasValue)
                source.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds.Value);

            if (!source.DefaultRequestHeaders.Contains("User-Agent"))
                source.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.Agent);

            return source;
        }
    }
}
