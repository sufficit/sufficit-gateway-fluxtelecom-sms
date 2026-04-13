namespace Sufficit.Gateway.FluxTelecom.SMS.Tests
{
    internal static class SampleHtml
    {
        public const string LoginHtml = @"<!DOCTYPE html>
<html>
<body>
    <form id=""UsuarioForm"">
        <input name=""usuarioVo.nomEmail"" />
        <input name=""usuarioVo.nomSenha"" />
    </form>
</body>
</html>";

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

        public const string UserListAjaxHtml = @"<table cellpadding=""0"" class=""listagem"" cellspacing=""0""><tbody>
    <tr>
        <td class=""icon""><a href=""javascript:excluirUsuario(11461, 'SUFFICIT')""><img src=""images/cross.png"" /></a></td>
        <td class=""text-left""><a href=""javascript:editar(11461)"">SUFFICIT (Adm. Empresa)</a></td>
        <td class=""text-left""><a href=""javascript:editar(11461)"">sufficit@massiva.net.br</a></td>
        <td class=""recoup""><a href=""javascript:editar(11461)"">Ativo</a></td>
    </tr>
</tbody></table>";

        public const string UserEditHtml = @"<form name=""UsuarioForm"" method=""post"" action=""/usuariocadastro.do"" enctype=""multipart/form-data"" id=""UsuarioForm"">
    <input type=""hidden"" name=""acao"" value=""EDITAR"" id=""acao"">
    <input type=""hidden"" name=""usuarioVo.idUsuario"" value=""11461"" id=""usuarioVo.idUsuario"">
    <input type=""hidden"" name=""empresaVo.idEmpresa"" value=""0"" id=""empresaVo.idEmpresa"">
    <input type=""hidden"" name=""usuarioVo.idEmpresa"" value=""6067"" id=""usuarioVo.idEmpresa"">
    <input type=""hidden"" name=""usuarioVo.nomEmailAux"" value=""sufficit@massiva.net.br"" id=""usuarioVo.nomEmailAux"">
    <input type=""hidden"" name=""usuarioVo.idCallback"" value="""" id=""usuarioVo.idCallback"">
    <input type=""hidden"" name=""usuarioVo.indStatusAux"" value=""1"" id=""usuarioVo.indStatusAux"">
    <input type=""hidden"" name=""usuarioVo.campoPalavraChaveMO"" value=""false"" id=""usuarioVo.campoPalavraChaveMO"">
    <input type=""hidden"" name=""tipoServicoVo.idsTipoServico"" value="""" id=""tipoServicoVo.idsTipoServico"">
    <input type=""hidden"" name=""tipoServicoVo.dscListaTipoServico"" value="""" id=""tipoServicoVo.dscListaTipoServico"">
    <input type=""text"" name=""usuarioVo.nomUsuario"" value=""SUFFICIT"" id=""usuarioVo.nomUsuario"">
    <input type=""text"" name=""usuarioVo.nomEmail"" value=""sufficit@massiva.net.br"" disabled=""disabled"" id=""usuarioVo.nomEmail"">
    <input type=""text"" name=""usuarioVo.nroDDD"" value="""" id=""usuarioVo.nroDDD"">
    <input type=""text"" name=""usuarioVo.nroTelefone"" value="""" id=""usuarioVo.nroTelefone"">
    <textarea name=""usuarioVo.nroIp"" id=""usuarioVo.nroIp"">187.108.200.77,45.233.44.244</textarea>
    <input type=""text"" name=""usuarioVo.nomRua"" value="""" id=""usuarioVo.nomRua"">
    <input type=""text"" name=""usuarioVo.nomBairro"" value="""" id=""usuarioVo.nomBairro"">
    <input type=""text"" name=""usuarioVo.codCep"" value="""" id=""usuarioVo.codCep"">
    <input type=""text"" name=""usuarioVo.nomMunicipio"" value="""" id=""usuarioVo.nomMunicipio"">
    <select name=""usuarioVo.siglaEstado"" id=""usuarioVo.siglaEstado""><option value="""" selected=""selected"">---</option><option value=""SP"">SP</option></select>
    <input type=""hidden"" name=""usuarioVo.indTipoCredito"" value=""2"">
    <input type=""radio"" name=""usuarioVo.indStatus"" value=""1"" checked=""checked"" id=""usuarioVo.indStatus1""><label>Ativo</label>
    <input type=""radio"" name=""usuarioVo.indStatus"" value=""0"" id=""usuarioVo.indStatus2""><label>Inativo</label>
    <input type=""checkbox"" name=""grupoAcessoVo.dynamicField(indStatus@idGrupo#7)"" checked=""true"" value=""1"" />
    <textarea name=""usuarioVo.observacao"" id=""usuarioVo.observacao""></textarea>
</form>";

        public const string ReplyListAjaxHtml = @"<table cellpadding=""0"" class=""listagem"" cellspacing=""0"">
    <thead>
        <tr>
            <th>SMS</th>
            <th>B/W</th>
            <th>Id Campanha</th>
            <th style=""width:150px"">Nome Campanha</th>
            <th style=""width:200px"">Centro de Custo</th>
            <th style=""width:200px"">Id Mensagem</th>
            <th style=""width:400px"">Mensagem Enviada</th>
            <th style=""width:400px"">Mensagem Resposta</th>
            <th style=""width:100px"">Telefone Remetente</th>
            <th style=""width:90px"">Data</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td><a href=""javascript:popupSms('0','63032547','5511999999999');""><img src=""images/sms_green.png"" title=""Enviar SMS"" style=""width: 15px;""/></a></td>
            <td><a href=""javascript:popupBWList('0','63032547','5511999999999');""><img src=""images/black_list-icon-green.png"" title=""Incluir Bloc/White list"" style=""width: 15px;""/></a></td>
            <td><a href=""javascript:verResposta(7282283051,2640364,63032547)"">2640364</a></td>
            <td><a title=""Mensagem Simples - 13/04/2026"" href=""javascript:verResposta(7282283051,2640364,63032547)"">Mensagem Simples - 13/04/2026</a></td>
            <td><a title=""Sem Centro de Custo"" href=""javascript:verResposta(7282283051,2640364,63032547)"">Sem Centro de Custo</a></td>
            <td><a title=""7282283051"" href=""javascript:verResposta(7282283051,2640364,63032547)"">7282283051</a></td>
            <td class=""text-left""><a href=""javascript:verResposta(7282283051,2640364,63032547)"">teste</a></td>
            <td class=""text-left""><a title=""Ok"" href=""javascript:verResposta(7282283051,2640364,63032547)"">Ok</a></td>
            <td><a title=""Nome Contato: Cliente Teste"" href=""javascript:verResposta(7282283051,2640364,63032547)"">5511999999999</a></td>
            <td class=""recoup""><a href=""javascript:verResposta(7282283051,2640364,63032547)"">13/04/2026 14:48:05</a></td>
        </tr>
    </tbody>
</table>";
    }
}
