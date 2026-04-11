namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal static class SampleHtml
    {
        public const string DashboardHtml = @"<!DOCTYPE html>
<html>
<body>
    <div class=""topo""><b>Seja bem vindo(a),</b><label>SUFFICIT</label></div>
    <div class=""credito""><label>330</label><b>Sms</b></div>
    <div id=""menu_nagegacao"">
        <ul>
            <li><a href=""campanhalista.do"">Ação de Envio</a></li>
            <li><a href=""listaftp.do"">Arquivos FTP</a></li>
            <li><a href=""pesquisatelefone.do"">Pesquisar Telefone</a></li>
        </ul>
    </div>
</body>
</html>";

        public const string AccessDeniedHtml = @"<html><body><h1>ActionException</h1><div>Acesso negado!</div></body></html>";

        public const string InvalidUrlHtml = @"<html><body><div>URL INVALIDA - contate o suporte</div></body></html>";

        public const string FtpAjaxHtml = @"<table cellpadding=""0"" cellspacing=""0"" class=""listagem"" id=""detail"">
<thead>
    <tr>
        <th></th>
        <th>Campanha</th>
        <th>Arquivo</th>
        <th>Inclusão</th>
        <th>Qtd Envios</th>
        <th>1 Proposta</th>
        <th>2 Propostas</th>
        <th>3 Propostas</th>
        <th>4 Propostas</th>
        <th>5 Propostas</th>
        <th>6 Propostas</th>
    </tr>
</thead>
<tbody>
    <tr>
        <td><input type=""button"" value=""Excluir"" /></td>
        <td>Campanha Teste</td>
        <td>contatos.csv</td>
        <td>11/04/2026 10:30</td>
        <td>120</td>
        <td>10</td>
        <td>20</td>
        <td>30</td>
        <td>40</td>
        <td>50</td>
        <td>60</td>
    </tr>
</tbody>
</table>";
    }
}
