using Xunit;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal sealed class FluxTelecomIntegrationFactAttribute : FactAttribute
    {
        public FluxTelecomIntegrationFactAttribute()
        {
            var settings = FluxTelecomIntegrationTestSettings.Load();
            if (!settings.IsConfigured)
                Skip = "Fill test/appsettings.json under Sufficit -> Gateway -> FluxTelecom -> Credentials with valid portal credentials before running integration tests.";
        }
    }

    internal sealed class FluxTelecomSendSmsIntegrationFactAttribute : FactAttribute
    {
        public FluxTelecomSendSmsIntegrationFactAttribute()
        {
            using var settings = FluxTelecomIntegrationTestSettings.Load();
            if (!settings.IsConfigured)
            {
                Skip = "Fill test/appsettings.json under Sufficit -> Gateway -> FluxTelecom -> Credentials with valid portal credentials before running send integration tests.";
                return;
            }

            if (!settings.HasUsableTestPhone)
                Skip = "Fill test/appsettings.json under Sufficit -> Gateway -> FluxTelecom -> TestPhone with a valid phone number before running send integration tests.";
        }
    }
}
