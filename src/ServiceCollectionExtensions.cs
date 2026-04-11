using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Dependency injection helpers for the Flux Telecom SMS gateway.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the gateway services without binding configuration values.
        /// </summary>
        /// <param name="services">Target service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddGatewayFluxTelecomSms(this IServiceCollection services)
        {
            services.AddOptions<GatewayOptions>();
            services.AddSingleton<FluxTelecomSmsClientFactory>();
            return services;
        }

        /// <summary>
        /// Registers the gateway services and binds <see cref="GatewayOptions"/> from the canonical configuration section.
        /// </summary>
        /// <param name="services">Target service collection.</param>
        /// <param name="configuration">Configuration root that contains <see cref="GatewayOptions.SECTIONNAME"/>.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddGatewayFluxTelecomSms(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<GatewayOptions>();
            services.Configure<GatewayOptions>(options =>
            {
                configuration.GetSection(GatewayOptions.SECTIONNAME).Bind(options);
            });
            services.AddSingleton<FluxTelecomSmsClientFactory>();
            return services;
        }
    }
}
