using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal sealed class FluxTelecomIntegrationTestSettings : IDisposable
    {
        private const string CREDENTIALS_SECTION = "Sufficit:Gateway:FluxTelecom:Credentials";
        private const string TEST_PHONE_SECTION = "Sufficit:Gateway:FluxTelecom:TestPhone";

        private readonly ServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        private FluxTelecomIntegrationTestSettings(ServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public GatewayOptions Gateway => _serviceProvider.GetRequiredService<IOptions<GatewayOptions>>().Value;

        public FluxTelecomCredentials Credentials => _serviceProvider.GetRequiredService<IOptions<FluxTelecomCredentials>>().Value;

        public string TestPhone => _configuration[TEST_PHONE_SECTION] ?? string.Empty;

        public bool IsConfigured => HasUsableValue(Credentials.Email)
            && HasUsableValue(Credentials.Password)
            && !string.IsNullOrWhiteSpace(Gateway.BaseUrl);

        public bool HasUsableTestPhone => HasUsablePhone(TestPhone);

        public FluxTelecomSmsClient CreateClient()
            => _serviceProvider.GetRequiredService<FluxTelecomSmsClientFactory>().Create(Credentials);

        public FluxTelecomSimpleMessageRecipient CreateTestRecipient(string name = "Test")
        {
            var normalized = NormalizeDigits(TestPhone);
            if (normalized.StartsWith("55", StringComparison.Ordinal) && normalized.Length >= 12)
                normalized = normalized.Substring(2);

            if (normalized.Length < 10 || normalized.Length > 11)
                throw new InvalidOperationException("TestPhone must contain a valid Brazilian mobile or landline number with area code.");

            return new FluxTelecomSimpleMessageRecipient()
            {
                AreaCode = normalized.Substring(0, 2),
                Number = normalized.Substring(2),
                Name = string.IsNullOrWhiteSpace(name) ? "Test" : name.Trim()
            };
        }

        public string CreateTimestampedTestMessage()
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            return string.Format(CultureInfo.InvariantCulture, "Teste Flux Telecom {0}", timestamp);
        }

        public static FluxTelecomIntegrationTestSettings Load()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.example.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddGatewayFluxTelecomSms(configuration);
            services.AddOptions<FluxTelecomCredentials>()
                .Configure(options =>
                {
                    var selected = LoadCredentials(configuration);
                    options.Email = selected.Email;
                    options.Password = selected.Password;
                });

            return new FluxTelecomIntegrationTestSettings(services.BuildServiceProvider(), configuration);
        }

        private static FluxTelecomCredentials LoadCredentials(IConfiguration configuration)
        {
            return BindCredentials(configuration, CREDENTIALS_SECTION);
        }

        private static FluxTelecomCredentials BindCredentials(IConfiguration configuration, string sectionName)
        {
            var credentials = new FluxTelecomCredentials();
            configuration.GetSection(sectionName).Bind(credentials);
            return credentials;
        }

        private static bool HasUsableValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return !value.StartsWith("fill-with-", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasUsablePhone(string? value)
        {
            if (!HasUsableValue(value))
                return false;

            var normalized = NormalizeDigits(value!);
            if (normalized.StartsWith("55", StringComparison.Ordinal) && normalized.Length >= 12)
                normalized = normalized.Substring(2);

            return normalized.Length >= 10 && normalized.Length <= 11;
        }

        private static string NormalizeDigits(string value)
            => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}
