using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    public class FluxTelecomSmsClientTests
    {
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