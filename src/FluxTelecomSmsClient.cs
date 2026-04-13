using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
        private const string JSON_SEND_URL = "http://apisms.fluxtelecom.com.br/envio";
        private const string MESSAGE_STATUS_QUERY_URL = "http://apisms.fluxtelecom.com.br/integracao3.do";
        private const string LOGIN_PATH = "login.do";
        private const string DASHBOARD_PATH = "campanhalista.do";
        private const string USER_LIST_PATH = "usuariolista.do";
        private const string USER_FORM_PATH = "usuariocadastro.do";
        private const string USER_UPDATE_PATH = "usuarioatualizar.do";
        private const string FTP_LIST_PATH = "listaftp.do";
        private const string FTP_UPLOAD_PATH = "ftpuploadcontato.do";
        private const string FTP_DELETE_PATH = "ftpdelete.do";
        private const string FTP_CAMPAIGN_CREATE_PATH = "ftpcampanhacadastro.do";
        private const string PHONE_SEARCH_AJAX_PATH = "pesquisatelefonejax.do";
        private const string PHONE_SEARCH_REPORT_PATH = "rptpesquisatelefone.do";
        private const string REPLY_LIST_PATH = "respostalista.do";
        private const string INTEGRATION_PATH = "integracao.do";
        private const string INTEGRATION_LIST_PATH = "integracaolista.do";
        private const string SIMPLE_MESSAGE_FORM_PATH = "mensagemsimplescadastro.do";
        private const string SIMPLE_MESSAGE_INSERT_PATH = "mensagemsimplesinsert.do";
        private const string STATUS_QUERY_ACCOUNT_FIELD = "account";
        private const string STATUS_QUERY_CODE_FIELD = "code";
        private const string STATUS_QUERY_TYPE_FIELD = "type";
        private const string STATUS_QUERY_IDS_FIELD = "id";
        private const string STATUS_QUERY_TYPE = "C";
        private const string JSON_HEADER_ACCOUNT = "account";
        private const string JSON_HEADER_CODE = "code";
        private const string SUCCESS_RESPONSE_CODE = "0";
        private const string INVALID_USER_RESPONSE_CODE = "900";
        private const string INVALID_USER_RESPONSE_DESCRIPTION = "USUARIO INVALIDO";
        private const string INVALID_JSON_PAYLOAD_RESPONSE = "FORMATO JSON INVALIDO";
        private const int MAX_ERROR_SNIPPET_LENGTH = 160;

        private const string ACTION_FIELD = "acao";
        private const string AJAX_ACTION = "AJAX";
        private const string UPLOAD_ACTION = "upload";
        private const string DELETE_ACTION = "excluirArquivo";
        private const string PHONE_SEARCH_ACTION = "PESQUISA";
        private const string LOGIN_ACTION = "logar";

        private const string LOGIN_EMAIL_FIELD = "usuarioVo.nomEmail";
        private const string LOGIN_PASSWORD_FIELD = "usuarioVo.nomSenha";
        private const string USER_ID_FIELD = "usuarioVo.idUsuario";
        private const string USER_NAME_FIELD = "usuarioVo.nomUsuario";
        private const string USER_EMAIL_AUX_FIELD = "usuarioVo.nomEmailAux";
        private const string USER_AUTHORIZED_IP_FIELD = "usuarioVo.nroIp";
        private const string USER_FORM_ID = "UsuarioForm";
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
        private const string REPLY_PAGE_FIELD = "respostaVo.pagina";
        private const string REPLY_START_FIELD = "respostaVo.datInicial";
        private const string REPLY_END_FIELD = "respostaVo.datFinal";
        private const string REPLY_PHONE_FIELD = "respostaVo.dddTelefone";
        private const string REPLY_COST_CENTER_IDS_FIELD = "centroCustoVo.idsCentroCusto";
        private const string REPLY_CAMPAIGN_IDS_FIELD = "campanhaVo.idsCampanhas";

        private readonly FluxTelecomCredentials _credentials;
        private readonly GatewayOptions _options;
        private readonly ILogger<FluxTelecomSmsClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        private static readonly JsonSerializerOptions MessageStatusSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions JsonPayloadSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        private static readonly Regex NumericTextRegex = new Regex("^\\d+$", RegexOptions.Compiled);

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

            var page = await ExecuteRawPageRequestAsync(
                LOGIN_PATH,
                BuildLoginRequest,
                cancellationToken).ConfigureAwait(false);

            if (FluxTelecomHtml.ContainsInvalidCredentials(page.Html))
                throw new FluxTelecomAuthenticationException("Unable to authenticate to Flux Telecom with the provided credentials.");

            if (!page.IsAuthenticated)
            {
                page = await ExecuteRawPageRequestAsync(
                    DASHBOARD_PATH,
                    () => new HttpRequestMessage(HttpMethod.Get, DASHBOARD_PATH),
                    cancellationToken).ConfigureAwait(false);
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
        /// Loads the AJAX user list available in the management area.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The parsed user rows rendered by the portal list partial.</returns>
        public async Task<IReadOnlyList<FluxTelecomPortalUserEntry>> ListUsersAsync(CancellationToken cancellationToken = default)
        {
            var page = await ExecutePageRequestAsync(
                USER_LIST_PATH,
                BuildUserListRequest,
                cancellationToken,
                true).ConfigureAwait(false);

            return FluxTelecomHtml.ParsePortalUsers(page.Html);
        }

        /// <summary>
        /// Ensures one or more IPv4 entries are present in the authorized IP list of an existing Flux Telecom portal user.
        /// </summary>
        /// <param name="request">Update request that identifies the user and the IPs to keep in the portal field.</param>
        /// <param name="cancellationToken">Cancellation token for the portal workflow.</param>
        /// <returns>The original and effective authorized IP lists after the reconciliation step.</returns>
        public async Task<FluxTelecomPortalUserAuthorizedIpUpdateResult> EnsureAuthorizedIpsAsync(FluxTelecomPortalUserAuthorizedIpUpdateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            var userId = await ResolvePortalUserIdAsync(request, cancellationToken).ConfigureAwait(false);
            var editFields = await GetUserEditFormFieldsAsync(userId, cancellationToken).ConfigureAwait(false);

            var userName = GetFieldValue(editFields, USER_NAME_FIELD) ?? string.Empty;
            var email = GetFieldValue(editFields, USER_EMAIL_AUX_FIELD) ?? string.Empty;
            var originalAuthorizedIps = FluxTelecomPortalUserAuthorizedIpUpdateRequest.NormalizeAuthorizedIpTokens(new[]
            {
                GetFieldValue(editFields, USER_AUTHORIZED_IP_FIELD) ?? string.Empty
            });

            var effectiveAuthorizedIps = request.ReplaceExisting
                ? new List<string>(request.GetNormalizedAuthorizedIps())
                : MergeAuthorizedIps(originalAuthorizedIps, request.GetNormalizedAuthorizedIps());

            if (AreEqual(originalAuthorizedIps, effectiveAuthorizedIps))
            {
                return new FluxTelecomPortalUserAuthorizedIpUpdateResult()
                {
                    UserId = userId,
                    UserName = userName,
                    Email = email,
                    WasUpdated = false,
                    OriginalAuthorizedIps = originalAuthorizedIps,
                    AuthorizedIps = effectiveAuthorizedIps
                };
            }

            SetFieldValue(editFields, USER_AUTHORIZED_IP_FIELD, string.Join(",", effectiveAuthorizedIps));

            var updatePage = await ExecutePageRequestAsync(
                USER_UPDATE_PATH,
                () => BuildUserUpdateRequest(editFields),
                cancellationToken,
                true).ConfigureAwait(false);

            if (updatePage.AccessDenied)
                throw new InvalidOperationException($"Flux Telecom denied access while updating portal user {userId}.");

            if (updatePage.InvalidUrl)
                throw new InvalidOperationException($"Flux Telecom returned an invalid portal route while updating user {userId}.");

            var confirmedFields = FluxTelecomHtml.ParseSuccessfulFormFields(updatePage.Html, USER_FORM_ID);
            if (confirmedFields.Count > 0)
            {
                var confirmedUserName = GetFieldValue(confirmedFields, USER_NAME_FIELD);
                if (!string.IsNullOrWhiteSpace(confirmedUserName))
                    userName = confirmedUserName;

                var confirmedEmail = GetFieldValue(confirmedFields, USER_EMAIL_AUX_FIELD);
                if (!string.IsNullOrWhiteSpace(confirmedEmail))
                    email = confirmedEmail;

                var confirmedAuthorizedIpText = GetFieldValue(confirmedFields, USER_AUTHORIZED_IP_FIELD);
                if (confirmedAuthorizedIpText != null)
                {
                    effectiveAuthorizedIps = new List<string>(FluxTelecomPortalUserAuthorizedIpUpdateRequest.NormalizeAuthorizedIpTokens(new[]
                    {
                        confirmedAuthorizedIpText
                    }));
                }
            }

            return new FluxTelecomPortalUserAuthorizedIpUpdateResult()
            {
                UserId = userId,
                UserName = userName,
                Email = email,
                WasUpdated = true,
                OriginalAuthorizedIps = originalAuthorizedIps,
                AuthorizedIps = effectiveAuthorizedIps
            };
        }

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
        /// Executes the portal-backed reply list query and parses the returned AJAX table rows.
        /// </summary>
        /// <param name="request">Reply-search filters accepted by the portal page.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>The parsed reply rows rendered by the authenticated portal list.</returns>
        public async Task<IReadOnlyList<FluxTelecomReplyEntry>> QueryRepliesAsync(FluxTelecomReplySearchRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            var page = await ExecutePageRequestAsync(
                REPLY_LIST_PATH,
                () => BuildReplySearchRequest(request),
                cancellationToken,
                true).ConfigureAwait(false);

            return FluxTelecomHtml.ParseReplies(page.Html);
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

                ThrowIfProviderReturnedError(result.Code, result.ReturnDescription, "message status query");

                return result;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("Flux Telecom returned an invalid JSON payload for message status query.", exception);
            }
        }

        /// <summary>
        /// Sends one message through the official Flux Telecom JSON POST API described in section 7.1 of the provider manual.
        /// </summary>
        /// <param name="request">Single JSON message payload.</param>
        /// <param name="cancellationToken">Cancellation token for the outbound request.</param>
        /// <returns>The parsed provider JSON response.</returns>
        public Task<FluxTelecomJsonSendResponse> SendJsonMessageAsync(FluxTelecomJsonMessageRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();
            return ExecuteJsonSendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Sends a grouped request through the official Flux Telecom JSON POST API described in section 7.2 of the provider manual.
        /// </summary>
        /// <param name="request">Grouped JSON payload.</param>
        /// <param name="cancellationToken">Cancellation token for the outbound request.</param>
        /// <returns>The parsed provider JSON response.</returns>
        public Task<FluxTelecomJsonSendResponse> SendJsonBatchAsync(FluxTelecomJsonBatchRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Validate();

            var body = new Dictionary<string, object>()
            {
                ["mensagens"] = request.Messages
            };

            return ExecuteJsonSendAsync(body, cancellationToken);
        }

        /// <summary>
        /// Parses the official Flux Telecom callback payload documented in section 7.3 of the provider manual.
        /// </summary>
        /// <param name="json">Raw callback JSON body received by the consumer endpoint.</param>
        /// <returns>The parsed callback payload.</returns>
        public FluxTelecomJsonCallbackPayload ParseJsonCallback(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Callback JSON is required.", nameof(json));

            try
            {
                var result = JsonSerializer.Deserialize<FluxTelecomJsonCallbackPayload>(json, MessageStatusSerializerOptions);
                if (result == null)
                    throw new InvalidOperationException("Flux Telecom returned an empty JSON payload for callback parsing.");

                return result;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("Flux Telecom returned an invalid JSON payload for callback parsing.", exception);
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

            var page = await ExecuteRawPageRequestAsync(relativePath, requestFactory, cancellationToken).ConfigureAwait(false);

            if (page.RequiresLogin && allowAuthenticationRetry)
            {
                _logger.LogDebug("Flux Telecom session expired while requesting {path}, retrying after re-authentication.", relativePath);
                _authenticated = false;
                await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                return await ExecutePageRequestAsync(relativePath, requestFactory, cancellationToken, false).ConfigureAwait(false);
            }

            return page;
        }

        private async Task<FluxTelecomPortalPage> ExecuteRawPageRequestAsync(string relativePath, Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
        {
            using var request = requestFactory();
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return FluxTelecomPortalPage.Create(relativePath, response.RequestMessage?.RequestUri?.ToString(), html);
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
            request.Content = BuildBrowserCompatibleMultipartContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, LOGIN_ACTION),
                new KeyValuePair<string, string>(LOGIN_EMAIL_FIELD, _credentials.Email ?? string.Empty),
                new KeyValuePair<string, string>(LOGIN_PASSWORD_FIELD, _credentials.Password ?? string.Empty)
            });

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

        private HttpRequestMessage BuildUserListRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, USER_LIST_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, AJAX_ACTION)
            });

            return request;
        }

        private HttpRequestMessage BuildUserEditRequest(int userId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, USER_FORM_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(USER_ID_FIELD, userId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            });

            return request;
        }

        private HttpRequestMessage BuildUserUpdateRequest(IEnumerable<KeyValuePair<string, string>> fields)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, USER_UPDATE_PATH);
            request.Content = BuildBrowserCompatibleMultipartContent(fields);
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

        private HttpRequestMessage BuildReplySearchRequest(FluxTelecomReplySearchRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, REPLY_LIST_PATH);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, AJAX_ACTION),
                new KeyValuePair<string, string>(REPLY_PAGE_FIELD, requestData.Page.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>(REPLY_START_FIELD, requestData.GetStartDateText()),
                new KeyValuePair<string, string>(REPLY_END_FIELD, requestData.GetEndDateText()),
                new KeyValuePair<string, string>(REPLY_PHONE_FIELD, requestData.GetNormalizedPhoneFilter()),
                new KeyValuePair<string, string>(REPLY_COST_CENTER_IDS_FIELD, requestData.GetNormalizedCostCenterIds()),
                new KeyValuePair<string, string>(REPLY_CAMPAIGN_IDS_FIELD, requestData.GetNormalizedCampaignIds())
            });

            return request;
        }

        private HttpRequestMessage BuildSimpleMessageInsertRequest(FluxTelecomSimpleMessageRequest requestData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, SIMPLE_MESSAGE_INSERT_PATH);
            request.Content = BuildBrowserCompatibleMultipartContent(new[]
            {
                new KeyValuePair<string, string>(ACTION_FIELD, string.Empty),
                new KeyValuePair<string, string>("breadcrumbs", string.Empty),
                new KeyValuePair<string, string>("campanhaMensagemVo.listaNumero", requestData.GetRecipientListToken()),
                new KeyValuePair<string, string>("respostaVo.idResposta", "0"),
                new KeyValuePair<string, string>("linkVo.idMensagem", string.Empty),
                new KeyValuePair<string, string>("linkVo.nroTelefoneRemetente", string.Empty),
                new KeyValuePair<string, string>("campanhaMensagemVo.idTipoServico", requestData.GetServiceIdText()),
                new KeyValuePair<string, string>("campanhaMensagemVo.idTipoSms", requestData.GetSmsTypeText()),
                new KeyValuePair<string, string>("campanhaMensagemVo.idTipoServicoCarta", "0"),
                new KeyValuePair<string, string>("campanhaMensagemVo.mudarServicoParaCarta", "NAO"),
                new KeyValuePair<string, string>("campanhaVo.indAtencao", string.Empty),
                new KeyValuePair<string, string>("campanhaVo.palavrasAtencaoSpam", string.Empty),
                new KeyValuePair<string, string>("campanhaVo.palavrasAtencaoWarn", string.Empty),
                new KeyValuePair<string, string>("campanhaVo.palavrasEncontradas", string.Empty),
                new KeyValuePair<string, string>("campanhaVo.idStatusMensagem", string.Empty),
                new KeyValuePair<string, string>("campanhaMensagemVo.tipoServicoEnvioAux", requestData.ServiceOption),
                new KeyValuePair<string, string>("campanhaMensagemVo.idCentroCusto", requestData.GetCostCenterText()),
                new KeyValuePair<string, string>("campanhaMensagemVo.indAgendar", requestData.GetScheduledFlag()),
                new KeyValuePair<string, string>("campanhaMensagemVo.datAgendamento", requestData.GetScheduledAtText()),
                new KeyValuePair<string, string>("grupoModeloMensagemVo.idGrupo", requestData.GroupModelId),
                new KeyValuePair<string, string>("modeloMensagemVo.idModelo", requestData.ModelId),
                new KeyValuePair<string, string>("campanhaMensagemVo.indLinkGerenciado", requestData.GetManagedLinkFlag()),
                new KeyValuePair<string, string>("campanhaMensagemVo.dscLinkGerenciado", requestData.ManagedLink ?? string.Empty),
                new KeyValuePair<string, string>("campanhaMensagemVo.indWhatsapp", requestData.GetWhatsAppFlag()),
                new KeyValuePair<string, string>("campanhaMensagemVo.telefoneWhatsapp", requestData.WhatsAppPhone ?? string.Empty),
                new KeyValuePair<string, string>("campanhaMensagemVo.txtWhatsapp", requestData.WhatsAppText ?? string.Empty),
                new KeyValuePair<string, string>("campanhaMensagemVo.txtMensagem", requestData.Message)
            });

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

        private async Task<FluxTelecomJsonSendResponse> ExecuteJsonSendAsync<TPayload>(TPayload payload, CancellationToken cancellationToken)
        {
            using var httpRequest = BuildJsonSendRequest(payload);
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            try
            {
                var result = JsonSerializer.Deserialize<FluxTelecomJsonSendResponse>(json, MessageStatusSerializerOptions);
                if (result == null)
                    throw new InvalidOperationException("Flux Telecom returned an empty JSON payload for JSON send.");

                ThrowIfProviderReturnedError(result.Code, result.ReturnDescription, "JSON send");

                return result;
            }
            catch (JsonException exception)
            {
                var plainTextSuccess = TryParseDocumentedPlainTextJsonSendSuccess(json, payload);
                if (plainTextSuccess != null)
                    return plainTextSuccess;

                throw new InvalidOperationException(BuildJsonSendFailureMessage(json), exception);
            }
        }

        private HttpRequestMessage BuildJsonSendRequest<TPayload>(TPayload payload)
        {
            var account = _credentials.GetResolvedAccount();
            var code = _credentials.GetResolvedCode();

            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Flux Telecom API credentials are not configured.");

            var json = JsonSerializer.Serialize(payload, JsonPayloadSerializerOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, JSON_SEND_URL);
            request.Headers.Add(JSON_HEADER_ACCOUNT, account);
            request.Headers.Add(JSON_HEADER_CODE, code);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }

        private static void ThrowIfProviderReturnedError(string? code, string? description, string operation)
        {
            var normalizedCode = NormalizeOptionalText(code);
            if (string.IsNullOrWhiteSpace(normalizedCode) || string.Equals(normalizedCode, SUCCESS_RESPONSE_CODE, StringComparison.OrdinalIgnoreCase))
                return;

            var normalizedDescription = NormalizeOptionalText(description);
            if (string.Equals(normalizedCode, INVALID_USER_RESPONSE_CODE, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedDescription, INVALID_USER_RESPONSE_DESCRIPTION, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Flux Telecom API rejected the configured account/code with code 900 (USUARIO INVALIDO). Check the configured Email and Password under Sufficit:Gateway:FluxTelecom:Credentials.");
            }

            if (string.IsNullOrWhiteSpace(normalizedDescription))
                throw new InvalidOperationException($"Flux Telecom returned provider code {normalizedCode} for {operation}.");

            throw new InvalidOperationException($"Flux Telecom returned provider code {normalizedCode} for {operation}: {normalizedDescription}.");
        }

        private static string BuildJsonSendFailureMessage(string payload)
        {
            var normalizedPayload = NormalizeOptionalText(payload);
            if (string.IsNullOrWhiteSpace(normalizedPayload))
                return "Flux Telecom returned an invalid JSON payload for JSON send.";

            if (string.Equals(normalizedPayload, INVALID_JSON_PAYLOAD_RESPONSE, StringComparison.OrdinalIgnoreCase))
                return "Flux Telecom rejected the JSON send payload with FORMATO JSON INVALIDO.";

            var tableCode = FluxTelecomHtml.ParseSingleTableCellText(payload);
            if (!string.IsNullOrWhiteSpace(tableCode) && NumericTextRegex.IsMatch(tableCode))
            {
                if (string.Equals(tableCode, INVALID_USER_RESPONSE_CODE, StringComparison.OrdinalIgnoreCase))
                    return "Flux Telecom API rejected the configured account/code with code 900 (USUARIO INVALIDO). Check the configured Email and Password under Sufficit:Gateway:FluxTelecom:Credentials.";

                return $"Flux Telecom returned HTML code {tableCode} for JSON send.";
            }

            var plainText = FluxTelecomHtml.ExtractText(payload);
            if (string.IsNullOrWhiteSpace(plainText))
                plainText = normalizedPayload;

            if (plainText.Length > MAX_ERROR_SNIPPET_LENGTH)
                plainText = plainText.Substring(0, MAX_ERROR_SNIPPET_LENGTH).TrimEnd();

            return $"Flux Telecom returned a non-JSON payload for JSON send: {plainText}.";
        }

        private static FluxTelecomJsonSendResponse? TryParseDocumentedPlainTextJsonSendSuccess<TPayload>(string payload, TPayload requestPayload)
        {
            var normalizedPayload = NormalizeOptionalText(payload);
            if (string.IsNullOrWhiteSpace(normalizedPayload))
                return null;

            if (normalizedPayload.IndexOf('<') >= 0 || normalizedPayload.IndexOf('>') >= 0)
                return null;

            if (ContainsWhitespace(normalizedPayload))
                return null;

            var partnerId = TryGetJsonSendPartnerId(requestPayload);
            if (!string.IsNullOrWhiteSpace(partnerId)
                && string.Equals(normalizedPayload, partnerId, StringComparison.Ordinal))
            {
                return new FluxTelecomJsonSendResponse()
                {
                    MessageId = normalizedPayload
                };
            }

            if (NumericTextRegex.IsMatch(normalizedPayload) && normalizedPayload.Length > 3)
            {
                return new FluxTelecomJsonSendResponse()
                {
                    MessageId = normalizedPayload
                };
            }

            return null;
        }

        private static string? TryGetJsonSendPartnerId<TPayload>(TPayload payload)
        {
            if (payload is FluxTelecomJsonMessageRequest messageRequest)
                return NormalizeOptionalText(messageRequest.PartnerId);

            return null;
        }

        private static bool ContainsWhitespace(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (char.IsWhiteSpace(value[index]))
                    return true;
            }

            return false;
        }

        private static string? NormalizeOptionalText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<int> ResolvePortalUserIdAsync(FluxTelecomPortalUserAuthorizedIpUpdateRequest request, CancellationToken cancellationToken)
        {
            if (request.UserId.HasValue && request.UserId.Value > 0)
                return request.UserId.Value;

            var users = await ListUsersAsync(cancellationToken).ConfigureAwait(false);
            var requestedEmail = request.Email?.Trim();
            foreach (var user in users)
            {
                if (string.Equals(user.Email, requestedEmail, StringComparison.OrdinalIgnoreCase))
                    return user.UserId;
            }

            throw new InvalidOperationException($"Unable to find a Flux Telecom portal user with e-mail '{requestedEmail}'.");
        }

        private async Task<List<KeyValuePair<string, string>>> GetUserEditFormFieldsAsync(int userId, CancellationToken cancellationToken)
        {
            var page = await ExecutePageRequestAsync(
                USER_FORM_PATH,
                () => BuildUserEditRequest(userId),
                cancellationToken,
                true).ConfigureAwait(false);

            if (page.AccessDenied)
                throw new InvalidOperationException($"Flux Telecom denied access while loading portal user {userId}.");

            if (page.InvalidUrl)
                throw new InvalidOperationException($"Flux Telecom returned an invalid portal route while loading user {userId}.");

            var fields = new List<KeyValuePair<string, string>>(FluxTelecomHtml.ParseSuccessfulFormFields(page.Html, USER_FORM_ID));
            if (fields.Count == 0)
                throw new InvalidOperationException($"Flux Telecom did not return the expected user edit form for user {userId}.");

            return fields;
        }

        private static string? GetFieldValue(IReadOnlyList<KeyValuePair<string, string>> fields, string fieldName)
        {
            for (var index = 0; index < fields.Count; index++)
            {
                if (string.Equals(fields[index].Key, fieldName, StringComparison.OrdinalIgnoreCase))
                    return fields[index].Value;
            }

            return null;
        }

        private static void SetFieldValue(List<KeyValuePair<string, string>> fields, string fieldName, string value)
        {
            for (var index = 0; index < fields.Count; index++)
            {
                if (!string.Equals(fields[index].Key, fieldName, StringComparison.OrdinalIgnoreCase))
                    continue;

                fields[index] = new KeyValuePair<string, string>(fields[index].Key, value);
                return;
            }

            fields.Add(new KeyValuePair<string, string>(fieldName, value));
        }

        private static List<string> MergeAuthorizedIps(IReadOnlyList<string> originalAuthorizedIps, IReadOnlyList<string> requestedAuthorizedIps)
        {
            var result = new List<string>(originalAuthorizedIps);
            var seen = new HashSet<string>(originalAuthorizedIps, StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < requestedAuthorizedIps.Count; index++)
            {
                var authorizedIp = requestedAuthorizedIps[index];
                if (seen.Add(authorizedIp))
                    result.Add(authorizedIp);
            }

            return result;
        }

        private static bool AreEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
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

        private static HttpContent BuildBrowserCompatibleMultipartContent(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var boundary = "----SufficitFluxBoundary" + Guid.NewGuid().ToString("N");
            var builder = new StringBuilder();

            foreach (var parameter in parameters)
            {
                builder
                    .Append("--")
                    .Append(boundary)
                    .Append("\r\n")
                    .Append("Content-Disposition: form-data; name=\"")
                    .Append(parameter.Key ?? string.Empty)
                    .Append("\"\r\n\r\n")
                    .Append(parameter.Value ?? string.Empty)
                    .Append("\r\n");
            }

            builder
                .Append("--")
                .Append(boundary)
                .Append("--\r\n");

            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(builder.ToString()));
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");
            return content;
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