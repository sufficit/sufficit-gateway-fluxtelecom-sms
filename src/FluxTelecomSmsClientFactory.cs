using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Factory used to create authenticated Flux Telecom SMS clients with the current gateway options.
    /// </summary>
    public class FluxTelecomSmsClientFactory
    {
        private readonly IOptionsMonitor<GatewayOptions> _optionsMonitor;
        private readonly ILoggerFactory _loggerFactory;

        public FluxTelecomSmsClientFactory(IOptionsMonitor<GatewayOptions> optionsMonitor, ILoggerFactory loggerFactory)
        {
            _optionsMonitor = optionsMonitor;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Creates a new client instance using the current gateway options snapshot.
        /// </summary>
        /// <param name="credentials">Portal credentials used to authenticate the session.</param>
        /// <returns>A new Flux Telecom SMS client.</returns>
        public FluxTelecomSmsClient Create(FluxTelecomCredentials credentials)
        {
            return new FluxTelecomSmsClient(
                credentials,
                _optionsMonitor.CurrentValue,
                _loggerFactory.CreateLogger<FluxTelecomSmsClient>());
        }
    }
}
