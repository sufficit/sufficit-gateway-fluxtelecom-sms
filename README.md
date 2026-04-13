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

- This repository models the validated portal-backed flows plus the official JSON send, status, and callback surfaces that were explicitly documented by the provider manual.
- The provider manual may still contain additional endpoints or account-specific permissions that were not validated in the live environment yet.
- A successful simple-send request confirms portal acceptance, not guaranteed handset delivery.
- The official provider PDF is intentionally not redistributed in this repository.

## Installation

The project can be referenced directly from source and is also prepared for official Sufficit NuGet packaging.

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

## Versioning

This repository now follows the official Sufficit package versioning pattern.

- `Debug`: fallback development version `1.99.0.0`
- `Release`: timestamped version `1.yy.MMdd.HHmm`

Example:

- `1.26.0411.2235`

NuGet package filenames may normalize numeric segments and therefore omit leading zeroes in the generated `.nupkg` name.

The package/build configuration now includes:

- `Debug`
- `Release`

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
						Number = "999999999",
						Name = "Sample Recipient"
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

### Delivery status query

The official provider manual documents a JSON status consultation flow in section `1.7` through `integracao3.do` with `type=C`.

This gateway now exposes that flow explicitly:

```csharp
var statusResponse = await client.QueryMessageStatusesAsync(new FluxTelecomMessageStatusQueryRequest
{
	Account = "cliente@cliente.com.br",
	Code = "senha",
	MessageIds = { 1637364991, 1625346410 }
});

foreach (var message in statusResponse.Messages)
{
	Console.WriteLine($"MessageId: {message.MessageId}");
	Console.WriteLine($"Status: {message.StatusDescription}");
	Console.WriteLine($"DeliveredAt: {message.DeliveredAtText}");
}
```

This JSON query uses the documented `account` and `code` parameters from the provider manual. It is independent from the authenticated portal session used by the portal-backed workflows.

### Official JSON send with callback

The official provider manual also documents a JSON POST API at `http://apisms.fluxtelecom.com.br/envio`.

This gateway now exposes both single and grouped JSON send methods, including the documented callback fields.

```csharp
var sendResponse = await client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest
{
	To = "5511999999999",
	SendType = "1",
	Message = "Sufficit JSON send test",
	PartnerId = "order-001",
	CallbackUrl = "https://example.com/sms/callback",
	CallbackToken = "token-001"
});

Console.WriteLine($"Provider message id: {sendResponse.MessageId}");
Console.WriteLine($"Return code: {sendResponse.Code}");
Console.WriteLine($"Return description: {sendResponse.ReturnDescription}");
```

When the provider answers `200 OK` with plain text instead of JSON, the client now treats that documented shape as an accepted send and copies the returned token into `MessageId`. In practice this token may be either the provider-generated numeric code or the same `PartnerId` echoed back by `/envio`.

For grouped sends:

```csharp
var batchResponse = await client.SendJsonBatchAsync(new FluxTelecomJsonBatchRequest
{
	Messages =
	{
		new FluxTelecomJsonMessageRequest
		{
			To = "5511999999999",
			SendType = "1",
			Message = "Batch message 1"
		},
		new FluxTelecomJsonMessageRequest
		{
			To = "5511888888888",
			SendType = "2",
			Message = "Batch message 2",
			PartnerId = "batch-002"
		}
	}
});
```

### Callback payload model

The callback workflow documented in section `7.3` can be parsed with the callback models already included in this project:

```csharp
var payload = client.ParseJsonCallback(rawJsonFromWebhook);

foreach (var item in payload.Messages)
{
	Console.WriteLine($"Status: {item.Status}");
	Console.WriteLine($"Partner id: {item.PartnerId}");
	Console.WriteLine($"Response: {item.ResponseText}");
}
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
- `QueryMessageStatusesAsync(...)`
- `SendJsonMessageAsync(...)`
- `SendJsonBatchAsync(...)`
- `ParseJsonCallback(...)`

## Delivery status note

The official provider manual documents a JSON status consultation flow in section `1.7` through `integracao3.do` with `type=C` and one or more message identifiers, and this client now models that path directly.

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

## NuGet package generation

The project is prepared for local package generation through the standard `Release` configuration.

Create the package locally with:

```bash
dotnet pack src/Sufficit.Gateway.FluxTelecom.SMS.csproj --configuration Release --output src/nupkgs
```

The generated `.nupkg` files will be written to:

- `src/nupkgs`

This repository also includes a GitHub Actions workflow that publishes packages to NuGet.org on pushes to `main`, using the repository secret `NUGET_API_KEY`.

## License

No open-source license file has been added yet.

Until a license is explicitly published, treat this repository as source-visible but not licensed for reuse.

## Support

Use the repository issues for bugs, validation gaps, or requests for additional provider flows.

## Related

- Provider domain: Flux Telecom SMS portal and official manual
