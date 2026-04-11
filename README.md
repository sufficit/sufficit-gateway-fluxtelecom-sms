# Sufficit.Gateway.FluxTelecom.SMS

Session-based .NET gateway for Flux Telecom SMS operations validated against the authenticated portal workflow at `https://sms.fluxtelecom.com.br/`.

## About

This project is a practical gateway, not an invented SDK.

The implementation is based on two verified sources:

- live authenticated portal behavior
- the official provider PDF documentation that confirms additional HTTP/JSON capabilities

When the provider account used during implementation could not access the restricted integration area inside the portal, the client stayed intentionally conservative: only workflows that were visible and testable were implemented.

## Features

- Authenticated portal session with cookie handling and automatic re-login when the session expires
- Dashboard loading with available credit parsing
- Simple SMS submission through the same validated portal workflow used by the web UI
- FTP file listing, upload, deletion, and campaign generation requests
- Phone search request and report download helpers
- Raw integration-page probing with access-denied and invalid-URL classification
- Dependency injection helpers based on `Microsoft.Extensions.Options`
- Unit tests for request building and HTML parsing
- Optional live integration tests driven by a local ignored `appsettings.json`

## Important limitations

- This repository currently models the portal-backed flows that were validated in a live session.
- The official manual documents extra JSON endpoints, but only the already verified portal-backed operations are implemented here.
- A successful simple-send request confirms portal acceptance, not guaranteed handset delivery.
- The official provider PDF is intentionally not redistributed in this repository.

## Installation

There is no published NuGet package at the moment. Reference the project directly or build the library from source.

```xml
<ProjectReference Include="..\sufficit-gateway-fluxtelecom-sms\src\Sufficit.Gateway.FluxTelecom.SMS.csproj" />
```

Build the library with:

```bash
dotnet build src/Sufficit.Gateway.FluxTelecom.SMS.csproj -c Release
```

The project targets:

- `netstandard2.0`
- `net7.0`
- `net9.0`

## Configuration

The canonical configuration root is:

- `Sufficit:Gateway:FluxTelecom:SMS`
- `Sufficit:Gateway:FluxTelecom:Credentials`
- `Sufficit:Gateway:FluxTelecom:TestPhone`

Minimal example:

```json
{
	"Sufficit": {
		"Gateway": {
			"FluxTelecom": {
				"SMS": {
					"BaseUrl": "https://sms.fluxtelecom.com.br/",
					"Agent": "Sufficit Flux Telecom SMS Gateway",
					"TimeoutSeconds": 30,
					"AllowInvalidServerCertificate": true
				},
				"Credentials": {
					"Email": "portal-user@example.com",
					"Password": "portal-password"
				},
				"TestPhone": "+5521999999999"
			}
		}
	}
}
```

`AllowInvalidServerCertificate` exists because the provider portal exposed an invalid TLS chain during validation.

## Usage

### Basic usage without DI

```csharp
using System;
using System.Collections.Generic;
using Sufficit.Gateway.FluxTelecom.SMS;

var options = new GatewayOptions
{
		BaseUrl = "https://sms.fluxtelecom.com.br/",
		Agent = "Sample App",
		TimeoutSeconds = 30,
		AllowInvalidServerCertificate = true
};

var credentials = new FluxTelecomCredentials
{
		Email = "portal-user@example.com",
		Password = "portal-password"
};

using var client = new FluxTelecomSmsClient(credentials, options);

var dashboard = await client.AuthenticateAsync();
Console.WriteLine($"Logged as {dashboard.UserName}");
Console.WriteLine($"Available credits: {dashboard.AvailableCredits}");

var sendResult = await client.SendSimpleMessageAsync(new FluxTelecomSimpleMessageRequest
{
		Message = $"Sufficit test {DateTime.UtcNow:yyyyMMdd-HHmmss}",
		Recipients = new List<FluxTelecomSimpleMessageRecipient>
		{
				new FluxTelecomSimpleMessageRecipient
				{
						AreaCode = "21",
						Number = "967609095",
						Name = "Test Phone"
				}
		}
});

Console.WriteLine($"Resolved URL: {sendResult.ResolvedUrl}");
Console.WriteLine($"Authenticated page after send: {sendResult.IsAuthenticated}");
```

### Using dependency injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sufficit.Gateway.FluxTelecom.SMS;

var configuration = new ConfigurationBuilder()
		.AddJsonFile("appsettings.json", optional: false)
		.Build();

var services = new ServiceCollection();
services.AddLogging();
services.AddGatewayFluxTelecomSms(configuration);

using var serviceProvider = services.BuildServiceProvider();

var credentials = new FluxTelecomCredentials
{
		Email = configuration["Sufficit:Gateway:FluxTelecom:Credentials:Email"]
				?? throw new InvalidOperationException("Missing credentials e-mail."),
		Password = configuration["Sufficit:Gateway:FluxTelecom:Credentials:Password"]
				?? throw new InvalidOperationException("Missing credentials password.")
};

var factory = serviceProvider.GetRequiredService<FluxTelecomSmsClientFactory>();
using var client = factory.Create(credentials);

var credits = await client.GetAvailableCreditsAsync();
Console.WriteLine($"Available credits: {credits}");
```

### Balance-only query

```csharp
var credits = await client.GetAvailableCreditsAsync();
Console.WriteLine($"Available credits: {credits}");
```

### Other validated operations

The client also exposes:

- `GetDashboardAsync()`
- `GetIntegrationPageAsync()`
- `GetIntegrationListPageAsync()`
- `ListFtpFilesAsync()`
- `UploadFtpFileAsync(...)`
- `DeleteFtpFileAsync(...)`
- `GenerateCampaignFromFtpFileAsync(...)`
- `SearchPhoneAsync(...)`
- `DownloadPhoneSearchReportAsync(...)`

## Delivery status note

The official provider manual documents a JSON status consultation flow in section `1.7` through `integracao3.do` with `type=C` and one or more message identifiers.

That delivery-status flow is intentionally documented in the public XML comments of this project, but it is not implemented yet in the current client surface.

## Testing

Unit tests:

```bash
dotnet test test/Sufficit.Gateway.FluxTelecom.SMS.Tests.csproj -c Release --filter "Category!=Integration"
```

Live integration tests use a local ignored file:

- tracked template: `test/appsettings.example.json`
- private local file: `test/appsettings.json`

After filling real portal credentials, run:

```bash
dotnet test test/Sufficit.Gateway.FluxTelecom.SMS.Tests.csproj -c Release
```

## License

No open-source license file has been added yet.

Until a license is explicitly published, treat this repository as source-visible but not licensed for reuse.

## Support

Use the repository issues for bugs, validation gaps, or requests for additional provider flows.

## Related

- Intended future integration target: `sufficit-exchange-worker`
- Provider domain: Flux Telecom SMS portal and official manual
