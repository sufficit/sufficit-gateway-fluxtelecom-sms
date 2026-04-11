using System.Threading.Tasks;
using Xunit;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    [Trait("Category", "Integration")]
    public class FluxTelecomSmsClientIntegrationTests
    {
        [FluxTelecomIntegrationFact]
        public async Task AuthenticateAsync_ReturnsDashboard_WhenLocalAppSettingsAreConfigured()
        {
            using var settings = FluxTelecomIntegrationTestSettings.Load();

            using var client = settings.CreateClient();
            var dashboard = await client.AuthenticateAsync();

            Assert.NotNull(dashboard);
            Assert.NotEmpty(dashboard.Html);
            Assert.NotEmpty(dashboard.MenuItems);
        }

        [FluxTelecomIntegrationFact]
        public async Task GetAvailableCreditsAsync_ReturnsCredits_WhenLocalAppSettingsAreConfigured()
        {
            using var settings = FluxTelecomIntegrationTestSettings.Load();

            using var client = settings.CreateClient();
            var credits = await client.GetAvailableCreditsAsync();

            Assert.True(credits.HasValue);
            Assert.True(credits.Value >= 0);
        }

        [FluxTelecomSendSmsIntegrationFact]
        public async Task SendSimpleMessageAsync_SendsTimestampedSms_WhenLocalAppSettingsAreConfigured()
        {
            using var settings = FluxTelecomIntegrationTestSettings.Load();

            using var client = settings.CreateClient();
            var result = await client.SendSimpleMessageAsync(new FluxTelecomSimpleMessageRequest()
            {
                Message = settings.CreateTimestampedTestMessage(),
                Recipients =
                {
                    settings.CreateTestRecipient("Hugo")
                }
            });

            Assert.NotNull(result);
            Assert.True(result.IsAuthenticated);
            Assert.False(result.RequiresLogin);
        }
    }
}