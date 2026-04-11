using System.Collections.Generic;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal sealed class RecordedRequest
    {
        public string Method { get; set; } = string.Empty;

        public string RequestUri { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
