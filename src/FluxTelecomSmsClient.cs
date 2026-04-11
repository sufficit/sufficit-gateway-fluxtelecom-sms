using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Session-based client for the Flux Telecom SMS portal plus the official JSON message status consultation documented by the provider.
    /// </summary>
    /// <remarks>
    /// The official API PDF v2.7.2 documents additional HTTP/JSON capabilities besides the authenticated portal.
    /// In particular, section 1.7 describes a JSON delivery-status query through <c>integracao3.do</c> with <c>type=C</c>
    /// and one or more message identifiers. This client keeps the validated portal-backed operations and now also exposes
    /// that official JSON consultation flow explicitly through <see cref="QueryMessageStatusesAsync(FluxTelecomMessageStatusQueryRequest, CancellationToken)"/>.
    /// </remarks>
    public class FluxTelecomSmsClient : IDisposable
    {
        private const string MESSAGE_STATUS_QUERY_URL = "http://apisms.fluxtelecom.com.br/integracao3.do";
        private const string LOGIN_PATH = "login.do";
        private const string DASHBOARD_PATH = "campanhalista.do";
        private const string FTP_LIST_PATH = "listaftp.do";
        private const string FTP_UPLOAD_PATH = "ftpuploadcontato.do";
        private const string FTP_DELETE_PATH = "ftpdelete.do";
        private const string FTP_CAMPAIGN_CREATE_PATH = "ftpcampanhacadastro.do";
        private const string PHONE_SEARCH_AJAX_PATH = "pesquisatelefonejax.do";
        private const string PHONE_SEARCH_REPORT_PATH = "rptpesquisatelefone.do";
        private const string INTEGRATION_PATH = "integracao.do";
        private const string INTEGRATION_LIST_PATH = "integracaolista.do";
        private const string SIMPLE_MESSAGE_FORM_PATH = "mensagemsimplescadastro.do";
        private const string SIMPLE_MESSAGE_INSERT_PATH = "mensagemsimplesinsert.do";
        private const string STATUS_QUERY_ACCOUNT_FIELD = "account";
        private const string STATUS_QUERY_CODE_FIELD = "code";
        private const string STATUS_QUERY_TYPE_FIELD = "type";
        private const string STATUS_QUERY_IDS_FIELD = "id";
        private const string STATUS_QUERY_TYPE = "C";

        private const string ACTION_FIELD = "acao";
        private const string AJAX_ACTION = "AJAX";
        private const string UPLOAD_ACTION = "upload";
        private const string DELETE_ACTION = "excluirArquivo";
        private const string PHONE_SEARCH_ACTION = "PESQUISA";
        private const string LOGIN_ACTION = "logar";

        private const string LOGIN_EMAIL_FIELD = "usuarioVo.nomEmail";
        private const string LOGIN_PASSWORD_FIELD = "usuarioVo.nomSenha";
        private const string INTEGRATION_ID_FIELD = "integracaoVo.idIntegracao";
        private const string SOURCE_FILE_FIELD = "integracaoVo.dscArquivoOrigem";
        private const string REMAINING_MESSAGES_FIELD = "integracaoVo.qtdMensagemAux";
        private const string PROPOSAL_TYPE_FIELD = "campanhaVo.tipoPropostaFTP";
        private const string FILE_INPUT_FIELD = "fileInput";
        private const string SEARCH_START_FIELD = "campanhaMensagemVo.datInicial";
        private const string SEARCH_END_FIELD = "campanhaMensagemVo.datFinal";
        private const string SEARCH_COST_CENTER_FIELD = "campanhaMensagemVo.idCentroCusto";
        private const string REPORT_COST_CENTER_FIELD = "campanhaVo.idCentroCusto";
        private const string REPORT_COST_CENTER_SELECTOR_FIELD = "centroCustoVo.idCentroCusto";
        private const string PHONE_FIELD = "dddTelefone";

        private readonly FluxTelecomCredentials _credentials;
        private readonly GatewayOptions _options;
        private readonly ILogger<FluxTelecomSmsClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        private static readonly JsonSerializerOptions MessageStatusSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        private bool _authenticated;
        private bool _disposed;

        /// <summary>
        /// Creates a client with an internally managed <see cref="HttpClient"/> configured from <see cref="GatewayOptions"/>.
        /// </summary>
        /// <param name="credentials">Portal credentials used to authenticate the session.</param>
        /// <param name="options">Gateway options that define the base URL, timeout, and TLS policy.</param>
        /// <param name="logger">Optional logger used for diagnostic messages.</param>
        public FluxTelecomSmsClient(FluxTelecomCredentials credentials, GatewayOptions options, ILogger<FluxTelecomSmsClient>? logger = null)
            : this(credentials, options, CreateDefaultHttpClient(options), logger ?? NullLogger<FluxTelecomSmsClient>.Instance, true)
        {
        }

        /// <summary>
        /// Creates a client using an externally managed <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="credentials">Portal credentials used to authenticate the session.</param>
        /// <param name="options">Gateway options that define the base URL, timeout, and TLS policy.</param>
        /// <param name="httpClient">Externally managed HTTP client instance.</param>
        /// <param name="logger">Optional logger used for diagnostic messages.</param>
        public FluxTelecomSmsClient(FluxTelecomCredentials credentials, GatewayOptions options, HttpClient httpClient, ILogger<FluxTelecomSmsClient>? logger = null)
            : this(credentials, options, httpClient, logger ?? NullLogger<FluxTelecomSmsClient>.Instance, false)
        {
        }

        internal FluxTelecomSmsClient(FluxTelecomCredentials credentials, GatewayOptions options, HttpClient httpClient, ILogger<FluxTelecomSmsClient> logger, bool ownsHttpClient = false)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = (httpClient ?? throw new ArgumentNullException(nameof(httpClient))).Configure(options);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownsHttpClient = ownsHttpClient;
        }

        /// <summary>
        /// Opens a portal session and returns the parsed dashboard information.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the login flow.</param>
        /// <returns>The parsed authenticated dashboard.</returns>
        public async Task<FluxTelecomDashboard> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var page = await ExecutePageRequestAsync(
                LOGIN_PATH,
                BuildLoginRequest,
                cancellationToken,
                false).ConfigureAwait(false);

            if (FluxTelecomHtml.ContainsInvalidCredentials(page.Html))
                throw new FluxTelecomAuthenticationException("Unable to authenticate to Flux Telecom with the provided credentials.");

            if (!page.IsAuthenticated)
            {
                page = await ExecutePageRequestAsync(
                    DASHBOARD_PATH,
                    () => new HttpRequestMessage(HttpMethod.Get, DASHBOARD_PATH),
                    cancellationToken,
                    false).ConfigureAwait(false);
            }

            if (FluxTelecomHtml.ContainsInvalidCredentials(page.Html) || page.RequiresLogin || !page.IsAuthenticated)
                throw new FluxTelecomAuthenticationException("Unable to authenticate to Flux Telecom with the provided credentials.");

            _authenticated = true;
            _logger.LogDebug("Authenticated successfully against Flux Telecom portal.");
            return FluxTelecomHtml.ParseDashboard(page.Html);
        }

        /// <summary>
        /// Loads the authenticated dashboard page.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the dashboard request.</param>
        /// <returns>The parsed authenticated dashboard.</returns>
        public async Task<FluxTelecomDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var page = await ExecuteAuthenticatedGetAsync(DASHBOARD_PATH, cancellationToken).ConfigureAwait(false);
            return FluxTelecomHtml.ParseDashboard(page.Html);
        }

        /// <summary>
        /// Returns the current SMS credits parsed from the authenticated dashboard.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the dashboard request.</param>
        /// <returns>The available credits reported by the authenticated portal header.</returns>
        public async Task<int?> GetAvailableCreditsAsync(CancellationToken cancellationToken = default)
        {
            var dashboard = await GetDashboardAsync(cancellationToken).ConfigureAwait(false);
            return dashboard.AvailableCredits;
        }

        /// <summary>
        /// Loads the raw integration page.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page.</returns>
        public Task<FluxTelecomPortalPage> GetIntegrationPageAsync(CancellationToken cancellationToken = default)
            => ExecuteAuthenticatedGetAsync(INTEGRATION_PATH, cancellationToken);

        /// <summary>
        /// Loads the raw integration list page and preserves access-denied markers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page.</returns>
        public Task<FluxTelecomPortalPage> GetIntegrationListPageAsync(CancellationToken cancellationToken = default)
            => ExecuteAuthenticatedGetAsync(INTEGRATION_LIST_PATH, cancellationToken);

        /// <summary>
        /// Loads the AJAX partial with the current FTP files.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>Parsed FTP rows rendered by the portal partial.</returns>
        public async Task<IReadOnlyList<FluxTelecomFtpFileEntry>> ListFtpFilesAsync(CancellationToken cancellationToken = default)
        {
            var page = await ExecutePageRequestAsync(
                FTP_LIST_PATH,
                BuildAjaxRequest,
                cancellationToken,
                true).ConfigureAwait(false);

            return FluxTelecomHtml.ParseFtpFiles(page.Html);
        }

        /// <summary>
        /// Uploads a CSV or XLSX contact file to the FTP area.
        /// </summary>
        /// <param name="request">Upload payload and file metadata.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page returned after upload.</returns>
        public Task<FluxTelecomPortalPage> UploadFtpFileAsync(FluxTelecomFtpUploadRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            return ExecutePageRequestAsync(
                FTP_UPLOAD_PATH,
                () => BuildUploadRequest(request),
                cancellationToken,
                true);
        }

        /// <summary>
        /// Deletes an uploaded FTP file by its integration identifier.
        /// </summary>
        /// <param name="integrationId">Portal integration identifier of the uploaded file.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page returned after deletion.</returns>
        public Task<FluxTelecomPortalPage> DeleteFtpFileAsync(int integrationId, CancellationToken cancellationToken = default)
        {
            if (integrationId <= 0)
                throw new ArgumentOutOfRangeException(nameof(integrationId), "IntegrationId must be greater than zero.");

            return ExecutePageRequestAsync(
                FTP_DELETE_PATH,
                () => BuildDeleteRequest(integrationId),
                cancellationToken,
                true);
        }

        /// <summary>
        /// Triggers campaign generation from a previously uploaded FTP file.
        /// </summary>
        /// <param name="request">Campaign generation payload.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page returned after campaign generation.</returns>
        public Task<FluxTelecomPortalPage> GenerateCampaignFromFtpFileAsync(FluxTelecomFtpCampaignRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            return ExecutePageRequestAsync(
                FTP_CAMPAIGN_CREATE_PATH,
                () => BuildGenerateCampaignRequest(request),
                cancellationToken,
                true);
        }

        /// <summary>
        /// Executes the AJAX phone search request and returns the raw HTML partial.
        /// </summary>
        /// <param name="request">Search filters accepted by the portal form.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page that contains the partial search result.</returns>
        public Task<FluxTelecomPortalPage> SearchPhoneAsync(FluxTelecomPhoneSearchRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            return ExecutePageRequestAsync(
                PHONE_SEARCH_AJAX_PATH,
                () => BuildPhoneSearchRequest(request),
                cancellationToken,
                true);
        }

        /// <summary>
        /// Downloads the phone search report using the same filter fields accepted by the portal form.
        /// </summary>
        /// <param name="request">Search filters accepted by the portal report export.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>Binary report payload returned by the portal.</returns>
        public Task<byte[]> DownloadPhoneSearchReportAsync(FluxTelecomPhoneSearchRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            return ExecuteBinaryRequestAsync(
                PHONE_SEARCH_REPORT_PATH,
                () => BuildPhoneSearchReportRequest(request),
                cancellationToken,
                true);
        }

        /// <summary>
        /// Sends a simple SMS through the same portal form used by the UI.
        /// </summary>
        /// <param name="request">Simple message payload serialized to the portal form fields.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The raw classified portal page returned after submission.</returns>
        /// <remarks>
        /// A successful submission through this method confirms acceptance by the portal workflow, not guaranteed handset delivery.
        /// Delivery confirmation can be consulted later through <see cref="QueryMessageStatusesAsync(FluxTelecomMessageStatusQueryRequest, CancellationToken)"/>.
        /// </remarks>
        public Task<FluxTelecomPortalPage> SendSimpleMessageAsync(FluxTelecomSimpleMessageRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            return ExecutePageRequestAsync(
                SIMPLE_MESSAGE_INSERT_PATH,
                () => BuildSimpleMessageInsertRequest(request),
                cancellationToken,
                true);
        }

        /// <summary>
        /// Queries one or more provider-generated message identifiers through the official JSON endpoint documented in section 1.7 of the provider manual.
        /// </summary>
        /// <param name="request">Official JSON query payload with <c>account</c>, <c>code</c>, and one or more message ids.</param>
        /// <param name="cancellationToken">Cancellation token for the outbound request.</param>
        /// <returns>The parsed provider JSON payload with the returned message statuses.</returns>
        /// <remarks>
        /// This method does not depend on an authenticated portal session. It uses the documented JSON endpoint directly.
        /// </remarks>
        public async Task<FluxTelecomMessageStatusResponse> QueryMessageStatusesAsync(FluxTelecomMessageStatusQueryRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            using var httpRequest = BuildMessageStatusQueryRequest(request);
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            try
            {
                var result = JsonSerializer.Deserialize<FluxTelecomMessageStatusResponse>(json, MessageStatusSerializerOptions);
                if (result == null)
                    throw new InvalidOperationException("Flux Telecom returned an empty JSON payload for message status query.");

                return result;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("Flux Telecom returned an invalid JSON payload for message status query.", exception);
            }
        }

        private async Task<FluxTelecomPortalPage> ExecuteAuthenticatedGetAsync(string relativePath, CancellationToken cancellationToken)
        {
            return await ExecutePageRequestAsync(
                relativePath,
                () => new HttpRequestMessage(HttpMethod.Get, relativePath),
                cancellationToken,
                true).ConfigureAwait(false);
        }

        private async Task<FluxTelecomPortalPage> ExecutePageRequestAsync(string relativePath, Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken, bool allowAuthenticationRetry)
        {
            await EnsureAuthenticatedAsync(relativePath, cancellationToken).ConfigureAwait(false);

            using var request = requestFactory();
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var page = FluxTelecomPortalPage.Create(relativePath, response.RequestMessage?.RequestUri?.ToString(), html);

            if (page.RequiresLogin && allowAuthenticationRetry)
            {
                _logger.LogDebug("Flux Telecom session expired while requesting {path}, retrying after re-authentication.", relativePath);
                _authenticated = false;
                await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                return await ExecutePageRequestAsync(relativePath, requestFactory, cancellationToken, false).ConfigureAwait(false);
            }

            return page;
        }

        private async Task<byte[]> ExecuteBinaryRequestAsync(string relativePath, Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken, bool allowAuthenticationRetry)
        {
            await EnsureAuthenticatedAsync(relativePath, cancellationToken).ConfigureAwait(false);

            using var request = requestFactory();
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var resolvedUrl = response.RequestMessage?.RequestUri?.ToString() ?? string.Empty;

            var isLogin = resolvedUrl.IndexOf(LOGIN_PATH, StringComparison.OrdinalIgnoreCase) >= 0;
            if (isLogin && allowAuthenticationRetry)
            {
                _authenticated = false;
                await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                return await ExecuteBinaryRequestAsync(relativePath, requestFactory, cancellationToken, false).ConfigureAwait(false);
            }

            return bytes;
        }

        private async Task EnsureAuthenticatedAsync(string relativePath, CancellationToken cancellationToken)
        {
            if (_authenticated || string.Equals(relativePath, LOGIN_PATH, StringComparison.OrdinalIgnoreCase))
                return;

            await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
        }

        private HttpRequestMessage BuildLoginRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, LOGIN_PATH);
            request.Content = new MultipartFormDataContent()
            {
                { new StringContent(LOGIN_ACTION, Encoding.UTF8), ACTION_FIELD },
                { new StringContent(_credentials.Email ?? string.Empty, Encoding.UTF8), LOGIN_EMAIL_FIELD },
                { new StringContent(_credentials.Password ?? string.Empty, Encoding.UTF8), LOGIN_PASSWORD_FIELD }
            };

            return request;
        }

        private HttpRequestMessage BuildAjaxRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, FTP_LIST_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, AJAX_ACTION)
            });

            return request;
        }

        private HttpRequestMessage BuildUploadRequest(FluxTelecomFtpUploadRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, FTP_UPLOAD_PATH);
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(UPLOAD_ACTION, Encoding.UTF8), ACTION_FIELD);
            content.Add(new StringContent(requestData.IntegrationId.ToString(System.Globalization.CultureInfo.InvariantCulture), Encoding.UTF8), INTEGRATION_ID_FIELD);
            content.Add(new StringContent(string.Empty, Encoding.UTF8), SOURCE_FILE_FIELD);
            content.Add(new StringContent("0", Encoding.UTF8), REMAINING_MESSAGES_FIELD);
            content.Add(new StringContent("0", Encoding.UTF8), PROPOSAL_TYPE_FIELD);

            var fileContent = new ByteArrayContent(requestData.Content);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(requestData.ContentType);
            content.Add(fileContent, FILE_INPUT_FIELD, requestData.FileName);

            request.Content = content;
            return request;
        }

        private HttpRequestMessage BuildDeleteRequest(int integrationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, FTP_DELETE_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, DELETE_ACTION),
                new KeyValuePair<string, string>(INTEGRATION_ID_FIELD, integrationId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(SOURCE_FILE_FIELD, string.Empty),
                new KeyValuePair<string, string>(REMAINING_MESSAGES_FIELD, "0"),
                new KeyValuePair<string, string>(PROPOSAL_TYPE_FIELD, "0")
            });

            return request;
        }

        private HttpRequestMessage BuildGenerateCampaignRequest(FluxTelecomFtpCampaignRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, FTP_CAMPAIGN_CREATE_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, DELETE_ACTION),
                new KeyValuePair<string, string>(INTEGRATION_ID_FIELD, requestData.IntegrationId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(PROPOSAL_TYPE_FIELD, requestData.ProposalType.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(REMAINING_MESSAGES_FIELD, requestData.RemainingMessages.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(SOURCE_FILE_FIELD, string.Empty)
            });

            return request;
        }

        private HttpRequestMessage BuildPhoneSearchRequest(FluxTelecomPhoneSearchRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, PHONE_SEARCH_AJAX_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, PHONE_SEARCH_ACTION),
                new KeyValuePair<string, string>(SEARCH_START_FIELD, requestData.GetStartDateText()),
                new KeyValuePair<string, string>(SEARCH_END_FIELD, requestData.GetEndDateText()),
                new KeyValuePair<string, string>(SEARCH_COST_CENTER_FIELD, requestData.CostCenterId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(PHONE_FIELD, requestData.GetNormalizedPhone())
            });

            return request;
        }

        private HttpRequestMessage BuildPhoneSearchReportRequest(FluxTelecomPhoneSearchRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, PHONE_SEARCH_REPORT_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, string.Empty),
                new KeyValuePair<string, string>(REPORT_COST_CENTER_FIELD, requestData.CostCenterId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(REPORT_COST_CENTER_SELECTOR_FIELD, requestData.CostCenterId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("campanhaVo.dataInicial", requestData.GetStartDateText()),
                new KeyValuePair<string, string>("campanhaVo.dataFinal", requestData.GetEndDateText()),
                new KeyValuePair<string, string>(PHONE_FIELD, requestData.GetNormalizedPhone())
            });

            return request;
        }

        private HttpRequestMessage BuildSimpleMessageInsertRequest(FluxTelecomSimpleMessageRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, SIMPLE_MESSAGE_INSERT_PATH);
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(string.Empty, Encoding.UTF8), ACTION_FIELD);
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "breadcrumbs");
            content.Add(new StringContent(requestData.GetRecipientListToken(), Encoding.UTF8), "campanhaMensagemVo.listaNumero");
            content.Add(new StringContent("0", Encoding.UTF8), "respostaVo.idResposta");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "linkVo.idMensagem");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "linkVo.nroTelefoneRemetente");
            content.Add(new StringContent(requestData.GetServiceIdText(), Encoding.UTF8), "campanhaMensagemVo.idTipoServico");
            content.Add(new StringContent(requestData.GetSmsTypeText(), Encoding.UTF8), "campanhaMensagemVo.idTipoSms");
            content.Add(new StringContent("0", Encoding.UTF8), "campanhaMensagemVo.idTipoServicoCarta");
            content.Add(new StringContent("NAO", Encoding.UTF8), "campanhaMensagemVo.mudarServicoParaCarta");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "campanhaVo.indAtencao");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "campanhaVo.palavrasAtencaoSpam");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "campanhaVo.palavrasAtencaoWarn");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "campanhaVo.palavrasEncontradas");
            content.Add(new StringContent(string.Empty, Encoding.UTF8), "campanhaVo.idStatusMensagem");
            content.Add(new StringContent(requestData.ServiceOption, Encoding.UTF8), "campanhaMensagemVo.tipoServicoEnvioAux");
            content.Add(new StringContent(requestData.GetCostCenterText(), Encoding.UTF8), "campanhaMensagemVo.idCentroCusto");
            content.Add(new StringContent(requestData.GetScheduledFlag(), Encoding.UTF8), "campanhaMensagemVo.indAgendar");
            content.Add(new StringContent(requestData.GetScheduledAtText(), Encoding.UTF8), "campanhaMensagemVo.datAgendamento");
            content.Add(new StringContent(requestData.GroupModelId, Encoding.UTF8), "grupoModeloMensagemVo.idGrupo");
            content.Add(new StringContent(requestData.ModelId, Encoding.UTF8), "modeloMensagemVo.idModelo");
            content.Add(new StringContent(requestData.GetManagedLinkFlag(), Encoding.UTF8), "campanhaMensagemVo.indLinkGerenciado");
            content.Add(new StringContent(requestData.ManagedLink ?? string.Empty, Encoding.UTF8), "campanhaMensagemVo.dscLinkGerenciado");
            content.Add(new StringContent(requestData.GetWhatsAppFlag(), Encoding.UTF8), "campanhaMensagemVo.indWhatsapp");
            content.Add(new StringContent(requestData.WhatsAppPhone ?? string.Empty, Encoding.UTF8), "campanhaMensagemVo.telefoneWhatsapp");
            content.Add(new StringContent(requestData.WhatsAppText ?? string.Empty, Encoding.UTF8), "campanhaMensagemVo.txtWhatsapp");
            content.Add(new StringContent(requestData.Message, Encoding.UTF8), "campanhaMensagemVo.txtMensagem");

            request.Content = content;
            return request;
        }

        private HttpRequestMessage BuildMessageStatusQueryRequest(FluxTelecomMessageStatusQueryRequest requestData)
        {
            var queryString = BuildQueryString(new[]
            {
                new KeyValuePair<string, string>(STATUS_QUERY_ACCOUNT_FIELD, requestData.Account),
                new KeyValuePair<string, string>(STATUS_QUERY_CODE_FIELD, requestData.Code),
                new KeyValuePair<string, string>(STATUS_QUERY_TYPE_FIELD, STATUS_QUERY_TYPE),
                new KeyValuePair<string, string>(STATUS_QUERY_IDS_FIELD, requestData.GetMessageIdsText())
            });

            var request = new HttpRequestMessage(HttpMethod.Get, MESSAGE_STATUS_QUERY_URL + "?" + queryString);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return request;
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var builder = new StringBuilder();
            var first = true;

            foreach (var parameter in parameters)
            {
                if (!first)
                    builder.Append('&');

                builder
                    .Append(Uri.EscapeDataString(parameter.Key ?? string.Empty))
                    .Append('=')
                    .Append(Uri.EscapeDataString(parameter.Value ?? string.Empty));

                first = false;
            }

            return builder.ToString();
        }

        private static HttpClient CreateDefaultHttpClient(GatewayOptions options)
        {
            var handler = new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            if (options.AllowInvalidServerCertificate)
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            return new HttpClient(handler, true).Configure(options);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Disposes the client and releases the internally owned HTTP resources when applicable.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_ownsHttpClient)
                _httpClient.Dispose();

            _disposed = true;
        }
    }
}