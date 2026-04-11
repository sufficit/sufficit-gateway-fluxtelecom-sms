using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            Requests.Add(new RecordedRequest()
            {
                Method = request.Method.Method,
                RequestUri = request.RequestUri?.ToString() ?? string.Empty,
                Body = body,
                ContentType = request.Content?.Headers?.ContentType?.ToString() ?? string.Empty
            });

            return _responseFactory(request);
        }
    }
}
