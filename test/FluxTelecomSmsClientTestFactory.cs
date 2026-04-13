using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal static class FluxTelecomSmsClientTestFactory
    {
        public static FluxTelecomSmsClient Create(RecordingHttpMessageHandler handler, FluxTelecomCredentials? credentials = null)
        {
            var options = new GatewayOptions()
            {
                BaseUrl = "https://example.test/",
                Agent = "Flux Telecom Gateway Tests",
                TimeoutSeconds = 30
            };

            var httpClient = new HttpClient(handler, false)
            {
                BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute)
            };

            credentials ??= new FluxTelecomCredentials()
            {
                Email = "portal@example.test",
                Password = "secret"
            };

            return new FluxTelecomSmsClient(
                credentials,
                options,
                httpClient,
                NullLogger<FluxTelecomSmsClient>.Instance);
        }
    }
}
