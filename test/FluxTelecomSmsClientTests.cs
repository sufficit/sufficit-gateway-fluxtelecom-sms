using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    public class FluxTelecomSmsClientTests
    {
        [Fact]
        public async Task AuthenticateAsync_FallsBackToDashboardWithoutRecursiveReauthentication()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/login.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.LoginHtml);

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var dashboard = await client.AuthenticateAsync();

            Assert.Equal("SUFFICIT", dashboard.UserName);
            Assert.Equal(330, dashboard.AvailableCredits);
            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("login.do", handler.Requests[0].RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("campanhalista.do", handler.Requests[1].RequestUri, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AuthenticateAsync_UsesBrowserCompatibleMultipartLoginPayload()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/login.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.LoginHtml);

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            await client.AuthenticateAsync();

            Assert.StartsWith("multipart/form-data; boundary=----SufficitFluxBoundary", handler.Requests[0].ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Content-Disposition: form-data; name=\"acao\"", handler.Requests[0].Body, StringComparison.Ordinal);
            Assert.Contains("Content-Disposition: form-data; name=\"usuarioVo.nomEmail\"", handler.Requests[0].Body, StringComparison.Ordinal);
            Assert.Contains("Content-Disposition: form-data; name=\"usuarioVo.nomSenha\"", handler.Requests[0].Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Content-Type: text/plain", handler.Requests[0].Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetDashboardAsync_ParsesCreditsAndMenuItems()
        {
            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var dashboard = await client.GetDashboardAsync();

            Assert.Equal("SUFFICIT", dashboard.UserName);
            Assert.Equal(330, dashboard.AvailableCredits);
            Assert.Contains("Ação de Envio", dashboard.MenuItems);
            Assert.Contains("Arquivos FTP", dashboard.MenuItems);
            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("login.do", handler.Requests[0].RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("campanhalista.do", handler.Requests[1].RequestUri, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAvailableCreditsAsync_ReturnsParsedCredits()
        {
            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var credits = await client.GetAvailableCreditsAsync();

            Assert.Equal(330, credits);
        }

        [Fact]
        public async Task GetIntegrationPageAsync_ReturnsInvalidUrlMarker()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/integracao.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.InvalidUrlHtml);

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var page = await client.GetIntegrationPageAsync();

            Assert.True(page.InvalidUrl);
            Assert.False(page.AccessDenied);
        }

        [Fact]
        public async Task GetIntegrationListPageAsync_ReturnsAccessDeniedMarker()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/integracaolista.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.AccessDeniedHtml);

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var page = await client.GetIntegrationListPageAsync();

            Assert.True(page.AccessDenied);
            Assert.False(page.InvalidUrl);
        }

        [Fact]
        public async Task ListFtpFilesAsync_PostsAjaxActionAndParsesRows()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/listaftp.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.FtpAjaxHtml);

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var items = await client.ListFtpFilesAsync();

            Assert.Single(items);
            Assert.Equal("Campanha Teste", items[0].Campaign);
            Assert.Equal("contatos.csv", items[0].FileName);
            Assert.Equal(120, items[0].TotalDeliveries);

            var ajaxRequest = handler.Requests.Last();
            Assert.Equal("POST", ajaxRequest.Method);
            Assert.EndsWith("listaftp.do", ajaxRequest.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("acao=AJAX", ajaxRequest.Body);
        }

        [Fact]
        public async Task UploadFtpFileAsync_PostsMultipartContentWithExpectedFields()
        {
            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            await client.UploadFtpFileAsync(new FluxTelecomFtpUploadRequest()
            {
                IntegrationId = 77,
                FileName = "contacts.csv",
                Content = Encoding.UTF8.GetBytes("number,name\n5511999999999,Test"),
                ContentType = "text/csv"
            });

            var uploadRequest = handler.Requests.Last();
            Assert.Equal("POST", uploadRequest.Method);
            Assert.EndsWith("ftpuploadcontato.do", uploadRequest.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("multipart/form-data", uploadRequest.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("upload", uploadRequest.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("77", uploadRequest.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("contacts.csv", uploadRequest.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteFtpFileAsync_PostsExpectedFormFields()
        {
            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            await client.DeleteFtpFileAsync(91);

            var request = handler.Requests.Last();
            var decoded = WebUtility.UrlDecode(request.Body);

            Assert.EndsWith("ftpdelete.do", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("acao=excluirArquivo", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("integracaoVo.idIntegracao=91", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("integracaoVo.dscArquivoOrigem=", decoded, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GenerateCampaignFromFtpFileAsync_PostsExpectedFormFields()
        {
            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            await client.GenerateCampaignFromFtpFileAsync(new FluxTelecomFtpCampaignRequest()
            {
                IntegrationId = 88,
                ProposalType = 3,
                RemainingMessages = 15
            });

            var request = handler.Requests.Last();
            var decoded = WebUtility.UrlDecode(request.Body);

            Assert.EndsWith("ftpcampanhacadastro.do", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("acao=excluirArquivo", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("integracaoVo.idIntegracao=88", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaVo.tipoPropostaFTP=3", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("integracaoVo.qtdMensagemAux=15", decoded, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchPhoneAsync_AndDownloadReportAsync_PostExpectedSearchFields()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/rptpesquisatelefone.do", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var filter = new FluxTelecomPhoneSearchRequest()
            {
                StartDate = new DateTime(2026, 4, 1),
                EndDate = new DateTime(2026, 4, 10),
                CostCenterId = 5,
                Phone = "(11) 99999-1234"
            };

            await client.SearchPhoneAsync(filter);
            var report = await client.DownloadPhoneSearchReportAsync(filter);

            Assert.Equal(4, report.Length);

            var searchRequest = handler.Requests.First(request => request.RequestUri.EndsWith("pesquisatelefonejax.do", StringComparison.OrdinalIgnoreCase));
            var searchDecoded = WebUtility.UrlDecode(searchRequest.Body);
            Assert.Contains("acao=PESQUISA", searchDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.datInicial=01/04/2026", searchDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.datFinal=10/04/2026", searchDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.idCentroCusto=5", searchDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dddTelefone=11999991234", searchDecoded, StringComparison.OrdinalIgnoreCase);

            var reportRequest = handler.Requests.First(request => request.RequestUri.EndsWith("rptpesquisatelefone.do", StringComparison.OrdinalIgnoreCase));
            var reportDecoded = WebUtility.UrlDecode(reportRequest.Body);
            Assert.Contains("campanhaVo.dataInicial=01/04/2026", reportDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaVo.dataFinal=10/04/2026", reportDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaVo.idCentroCusto=5", reportDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("centroCustoVo.idCentroCusto=5", reportDecoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dddTelefone=11999991234", reportDecoded, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task QueryRepliesAsync_PostsExpectedFieldsAndParsesRows()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                var requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;

                if (request.RequestUri!.AbsolutePath.EndsWith("/respostalista.do", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(request.Method.Method, "POST", StringComparison.OrdinalIgnoreCase)
                    && requestBody.IndexOf("acao=AJAX", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CreateHtmlResponse(request, SampleHtml.ReplyListAjaxHtml);
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var filter = new FluxTelecomReplySearchRequest()
            {
                StartDate = new DateTime(2026, 4, 3),
                EndDate = new DateTime(2026, 4, 13),
                Page = 2,
                PhoneFilter = "(11) 99999-9999, 2133334444",
                CostCenterIds = "5, 7",
                CampaignIds = "2640364, 2640999"
            };

            var items = await client.QueryRepliesAsync(filter);

            Assert.Single(items);
            Assert.Equal(63032547, items[0].ReplyId);
            Assert.Equal(2640364, items[0].CampaignId);
            Assert.Equal(7282283051, items[0].MessageId);
            Assert.Equal("Mensagem Simples - 13/04/2026", items[0].CampaignName);
            Assert.Equal("Sem Centro de Custo", items[0].CostCenterDescription);
            Assert.Equal("teste", items[0].SentMessage);
            Assert.Equal("Ok", items[0].ReplyText);
            Assert.Equal("5511999999999", items[0].SenderPhone);
            Assert.Equal("Cliente Teste", items[0].SenderContactHint);
            Assert.Equal("13/04/2026 14:48:05", items[0].ReceivedAtText);

            var request = handler.Requests.First(httpRequest => httpRequest.RequestUri.EndsWith("respostalista.do", StringComparison.OrdinalIgnoreCase)
                && string.Equals(httpRequest.Method, "POST", StringComparison.OrdinalIgnoreCase)
                && httpRequest.Body.IndexOf("acao=AJAX", StringComparison.OrdinalIgnoreCase) >= 0);
            var decoded = WebUtility.UrlDecode(request.Body);

            Assert.Contains("acao=AJAX", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("respostaVo.pagina=2", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("respostaVo.datInicial=03/04/2026", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("respostaVo.datFinal=13/04/2026", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("respostaVo.dddTelefone=11999999999,2133334444", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("centroCustoVo.idsCentroCusto=5,7", decoded, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaVo.idsCampanhas=2640364,2640999", decoded, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ListUsersAsync_ParsesPortalUserRows()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/usuariolista.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.UserListAjaxHtml);

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var users = await client.ListUsersAsync();

            Assert.Single(users);
            Assert.Equal(11461, users[0].UserId);
            Assert.Equal("SUFFICIT (Adm. Empresa)", users[0].Name);
            Assert.Equal("sufficit@massiva.net.br", users[0].Email);
            Assert.True(users[0].IsActive);

            var request = handler.Requests.Last();
            Assert.EndsWith("usuariolista.do", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("acao=AJAX", request.Body);
        }

        [Fact]
        public async Task EnsureAuthorizedIpsAsync_MergesRequestedIpsIntoExistingPortalUser()
        {
            const string mergedIpList = "187.108.200.77,45.233.44.244,143.208.224.20";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/usuariolista.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.UserListAjaxHtml);

                if (request.RequestUri.AbsolutePath.EndsWith("/usuariocadastro.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.UserEditHtml);

                if (request.RequestUri.AbsolutePath.EndsWith("/usuarioatualizar.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.UserEditHtml.Replace("187.108.200.77,45.233.44.244", mergedIpList, StringComparison.Ordinal));

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var result = await client.EnsureAuthorizedIpsAsync(new FluxTelecomPortalUserAuthorizedIpUpdateRequest()
            {
                Email = "sufficit@massiva.net.br",
                AuthorizedIps = { "143.208.224.20" }
            });

            Assert.True(result.WasUpdated);
            Assert.Equal(11461, result.UserId);
            Assert.Equal("SUFFICIT", result.UserName);
            Assert.Equal("sufficit@massiva.net.br", result.Email);
            Assert.Equal(2, result.OriginalAuthorizedIps.Count);
            Assert.Equal(3, result.AuthorizedIps.Count);
            Assert.Contains("143.208.224.20", result.AuthorizedIps);

            var updateRequest = handler.Requests.Last();
            Assert.EndsWith("usuarioatualizar.do", updateRequest.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("multipart/form-data", updateRequest.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Content-Disposition: form-data; name=\"usuarioVo.idUsuario\"", updateRequest.Body, StringComparison.Ordinal);
            Assert.Contains("Content-Disposition: form-data; name=\"usuarioVo.nroIp\"", updateRequest.Body, StringComparison.Ordinal);
            Assert.Contains(mergedIpList, updateRequest.Body, StringComparison.Ordinal);
            Assert.Contains("Content-Disposition: form-data; name=\"grupoAcessoVo.dynamicField(indStatus@idGrupo#7)\"", updateRequest.Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task EnsureAuthorizedIpsAsync_SkipsPortalUpdateWhenRequestedIpsAlreadyExist()
        {
            const string currentIpList = "187.108.200.77,45.233.44.244,143.208.224.20";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/usuariolista.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.UserListAjaxHtml);

                if (request.RequestUri.AbsolutePath.EndsWith("/usuariocadastro.do", StringComparison.OrdinalIgnoreCase))
                    return CreateHtmlResponse(request, SampleHtml.UserEditHtml.Replace("187.108.200.77,45.233.44.244", currentIpList, StringComparison.Ordinal));

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var result = await client.EnsureAuthorizedIpsAsync(new FluxTelecomPortalUserAuthorizedIpUpdateRequest()
            {
                Email = "sufficit@massiva.net.br",
                AuthorizedIps = { "143.208.224.20" }
            });

            Assert.False(result.WasUpdated);
            Assert.Equal(3, result.AuthorizedIps.Count);
            Assert.DoesNotContain(handler.Requests, request => request.RequestUri.EndsWith("usuarioatualizar.do", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SendSimpleMessageAsync_PostsExpectedPortalFields()
        {
            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            await client.SendSimpleMessageAsync(new FluxTelecomSimpleMessageRequest()
            {
                Message = "Teste Flux Telecom",
                Recipients =
                {
                    new FluxTelecomSimpleMessageRecipient()
                    {
                        AreaCode = "21",
                        Number = "967609095",
                        Name = "Hugo"
                    }
                }
            });

            var request = handler.Requests.Last();

            Assert.EndsWith("mensagemsimplesinsert.do", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("multipart/form-data", request.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.tipoServicoEnvioAux", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("7103;2;160", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.indAgendar", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.indLinkGerenciado", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.indWhatsapp", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campanhaMensagemVo.txtMensagem", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Teste Flux Telecom", request.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("21_967609095_Hugo|", request.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task QueryMessageStatusesAsync_UsesOfficialJsonEndpointAndParsesMessages()
        {
            const string json = "{\"mensagens\":[{\"id_mensagem\":1637364991,\"data_entrega\":\"08/10/2019 14:09:00\",\"id_status\":120,\"data_inclusao\":\"08/10/2019 14:09:41\",\"id_parceiro\":\"1\",\"data_envio\":\"08/10/2019 14:09:43\",\"descricao_status\":\"MENSAGEM ENTREGUE\"},{\"id_mensagem\":1625346410,\"data_entrega\":\"13/09/2019 16:21:00\",\"id_status\":120,\"data_inclusao\":\"13/09/2019 16:21:33\",\"id_parceiro\":\"2\",\"data_envio\":\"13/09/2019 16:21:38\",\"descricao_status\":\"MENSAGEM ENTREGUE\"}]}";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/integracao3.do", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var response = await client.QueryMessageStatusesAsync(new FluxTelecomMessageStatusQueryRequest()
            {
                Account = "cliente@cliente.com.br",
                Code = "senha",
                MessageIds = { 1, 2 }
            });

            Assert.Equal(2, response.Messages.Count);
            Assert.Equal(1637364991, response.Messages[0].MessageId);
            Assert.Equal("MENSAGEM ENTREGUE", response.Messages[0].StatusDescription);
            Assert.Equal("2", response.Messages[1].PartnerId);

            Assert.Single(handler.Requests);

            var request = handler.Requests[0];
            Assert.Equal("GET", request.Method);
            Assert.StartsWith("http://apisms.fluxtelecom.com.br/integracao3.do?", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("account=cliente%40cliente.com.br", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("code=senha", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("type=C", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("id=1%3B2", request.RequestUri, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(string.Empty, request.Body);
        }

        [Fact]
        public async Task QueryMessageStatusesAsync_ThrowsWhenProviderReturnsInvalidUser()
        {
            const string json = "{\"codigo\":\"900\",\"descricao_retorno\":\"USUARIO INVALIDO\"}";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/integracao3.do", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.QueryMessageStatusesAsync(new FluxTelecomMessageStatusQueryRequest()
            {
                Account = "cliente@cliente.com.br",
                Code = "senha",
                MessageIds = { 900 }
            }));

            Assert.Contains("USUARIO INVALIDO", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Email", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendJsonMessageAsync_PostsOfficialJsonPayloadWithCallbackHeaders()
        {
            const string json = "{\"id_mensagem\":1726868950}";
            const string callbackUrl = "https://endpoints.sufficit.com.br/Gateway/FluxTelecomSms/Callback";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var response = await client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest()
            {
                To = "5511999999999",
                SendType = "1",
                Message = "Test callback message",
                PartnerId = "envio-json-001",
                CallbackUrl = callbackUrl,
                CallbackToken = "callback-token"
            });

            Assert.Equal("1726868950", response.MessageId);
            Assert.Single(handler.Requests);

            var request = handler.Requests[0];
            Assert.Equal("POST", request.Method);
            Assert.Equal("application/json; charset=utf-8", request.ContentType);
            Assert.Equal("portal@example.test", request.Headers["account"]);
            Assert.Equal("secret", request.Headers["code"]);

            using var document = JsonDocument.Parse(request.Body);
            var root = document.RootElement;
            Assert.Equal("5511999999999", root.GetProperty("to").GetString());
            Assert.Equal("1", root.GetProperty("tipoEnvio").GetString());
            Assert.Equal("Test callback message", root.GetProperty("msg").GetString());
            Assert.Equal("envio-json-001", root.GetProperty("id").GetString());
            Assert.Equal(callbackUrl, root.GetProperty("urlCallback").GetString());
            Assert.Equal("callback-token", root.GetProperty("tokenCallback").GetString());
        }

        [Fact]
        public async Task SendJsonMessageAsync_AcceptsPlainTextPartnerIdEchoAsSuccessfulResponse()
        {
            const string partnerId = "diag-json-20260413-121300";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(partnerId, Encoding.UTF8, "text/html")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var response = await client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest()
            {
                To = "5511999999999",
                SendType = "1",
                Message = "Test plain text partner id",
                PartnerId = partnerId
            });

            Assert.Equal(partnerId, response.MessageId);
            Assert.Null(response.Code);
            Assert.Null(response.ReturnDescription);
        }

        [Fact]
        public async Task SendJsonMessageAsync_AcceptsPlainTextGeneratedMessageCodeAsSuccessfulResponse()
        {
            const string providerMessageId = "2026041317760944705234924";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(providerMessageId, Encoding.UTF8, "text/html")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var response = await client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest()
            {
                To = "5511999999999",
                SendType = "1",
                Message = "Test plain text generated id"
            });

            Assert.Equal(providerMessageId, response.MessageId);
            Assert.Null(response.Code);
            Assert.Null(response.ReturnDescription);
        }

        [Fact]
        public async Task SendJsonMessageAsync_UsesPortalCredentialsAsAccountAndCodeHeaders()
        {
            const string json = "{\"id_mensagem\":\"1726868951\"}";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler, new FluxTelecomCredentials()
            {
                Email = "portal@example.test",
                Password = "secret"
            });

            await client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest()
            {
                To = "5511999999999",
                SendType = "1",
                Message = "Test API credentials"
            });

            var request = handler.Requests[0];
            Assert.Equal("portal@example.test", request.Headers["account"]);
            Assert.Equal("secret", request.Headers["code"]);
        }

        [Fact]
        public async Task SendJsonMessageAsync_ThrowsHelpfulMessageForPlainTextProviderError()
        {
            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent("FORMATO JSON INVALIDO", Encoding.UTF8, "text/html")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest()
            {
                To = "5511999999999",
                SendType = "1",
                Message = "Test plain text error"
            }));

            Assert.Contains("FORMATO JSON INVALIDO", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendJsonMessageAsync_ThrowsWhenProviderReturnsInvalidUserCode()
        {
            const string json = "{\"codigo\":\"900\",\"descricao_retorno\":\"USUARIO INVALIDO\"}";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendJsonMessageAsync(new FluxTelecomJsonMessageRequest()
            {
                To = "5511999999999",
                SendType = "1",
                Message = "Test invalid user code"
            }));

            Assert.Contains("USUARIO INVALIDO", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Email", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendJsonBatchAsync_PostsGroupedMessagesArray()
        {
            const string json = "{\"codigo\":\"0\",\"descricao_retorno\":\"SUCESSO\"}";

            using var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("http://apisms.fluxtelecom.com.br/envio", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return CreateHtmlResponse(request, SampleHtml.DashboardHtml);
            });

            using var client = FluxTelecomSmsClientTestFactory.Create(handler);
            var response = await client.SendJsonBatchAsync(new FluxTelecomJsonBatchRequest()
            {
                Messages =
                {
                    new FluxTelecomJsonMessageRequest()
                    {
                        To = "5511999999999",
                        SendType = "1",
                        Message = "Batch 1"
                    },
                    new FluxTelecomJsonMessageRequest()
                    {
                        To = "5511888888888",
                        SendType = "2",
                        Message = "Batch 2",
                        PartnerId = "lote-002"
                    }
                }
            });

            Assert.Equal("0", response.Code);
            Assert.Equal("SUCESSO", response.ReturnDescription);

            using var document = JsonDocument.Parse(handler.Requests[0].Body);
            var messages = document.RootElement.GetProperty("mensagens");
            Assert.Equal(2, messages.GetArrayLength());
            Assert.Equal("Batch 1", messages[0].GetProperty("msg").GetString());
            Assert.Equal("lote-002", messages[1].GetProperty("id").GetString());
        }

        [Fact]
        public void ParseJsonCallback_ParsesDeliveryAndReplyEntries()
        {
            const string json = "{\"mensagens\":[{\"telefone\":\"5567999036368\",\"data\":\"2020-02-12 21:00:04\",\"id\":1726868950,\"idParceiro\":\"5849682\",\"status\":\"ENTREGUE\"},{\"telefone\":\"5567999036368\",\"data\":\"2020-02-12 21:13:04\",\"resposta\":\"ok, obrigado\",\"id\":1726868950,\"idParceiro\":\"5849682\",\"status\":\"RESPOSTA\"}]}";

            using var handler = new RecordingHttpMessageHandler(request => CreateHtmlResponse(request, SampleHtml.DashboardHtml));
            using var client = FluxTelecomSmsClientTestFactory.Create(handler);

            var payload = client.ParseJsonCallback(json);

            Assert.Equal(2, payload.Messages.Count);
            Assert.Equal("ENTREGUE", payload.Messages[0].Status);
            Assert.Equal("RESPOSTA", payload.Messages[1].Status);
            Assert.Equal("ok, obrigado", payload.Messages[1].ResponseText);
        }

        private static HttpResponseMessage CreateHtmlResponse(HttpRequestMessage request, string html)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }
    }
}