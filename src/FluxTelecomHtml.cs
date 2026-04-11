using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    internal static class FluxTelecomHtml
    {
        private static readonly Regex HtmlTagRegex = new Regex("<.*?>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex MultiWhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex UserNameRegex = new Regex("Seja bem vindo\\(a\\),</b>\\s*<label>(.*?)</label>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex CreditsRegex = new Regex("<label\\s*>(\\d+)\\s*</label>\\s*<b\\s*>\\s*Sms\\s*</b>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex MenuAnchorRegex = new Regex("<a\\s+href=\"[^\"]*\">(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex TableRowRegex = new Regex("<tr[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex TableCellRegex = new Regex("<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public static bool IsLoginPage(string? html)
        {
            var source = html ?? string.Empty;
            return source.IndexOf("id=\"UsuarioForm\"", StringComparison.OrdinalIgnoreCase) >= 0
                && source.IndexOf("usuarioVo.nomEmail", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool ContainsInvalidCredentials(string? html)
            => (html ?? string.Empty).IndexOf("Usuário ou senha inválidos", StringComparison.OrdinalIgnoreCase) >= 0
            || (html ?? string.Empty).IndexOf("Usuário ou senha invalidos", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool ContainsAccessDenied(string? html)
            => (html ?? string.Empty).IndexOf("Acesso negado", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool ContainsInvalidUrl(string? html)
            => (html ?? string.Empty).IndexOf("URL INVALIDA - contate o suporte", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool ContainsAuthenticatedMarker(string? html)
        {
            var source = html ?? string.Empty;
            return source.IndexOf("Seja bem vindo(a)", StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("menu_nagegacao", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static FluxTelecomDashboard ParseDashboard(string html)
        {
            return new FluxTelecomDashboard()
            {
                Html = html ?? string.Empty,
                UserName = ParseUserName(html) ?? string.Empty,
                AvailableCredits = ParseCredits(html),
                MenuItems = ParseMenuItems(html)
            };
        }

        public static string? ParseUserName(string? html)
        {
            var match = UserNameRegex.Match(html ?? string.Empty);
            if (!match.Success)
                return null;

            return DecodeText(match.Groups[1].Value);
        }

        public static int? ParseCredits(string? html)
        {
            var match = CreditsRegex.Match(html ?? string.Empty);
            if (!match.Success)
                return null;

            return int.TryParse(match.Groups[1].Value, out var credits) ? credits : null;
        }

        public static IReadOnlyList<string> ParseMenuItems(string? html)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in MenuAnchorRegex.Matches(html ?? string.Empty))
            {
                var text = DecodeText(match.Groups[1].Value);
                if (string.IsNullOrWhiteSpace(text) || text == "#")
                    continue;

                if (seen.Add(text))
                    result.Add(text);
            }

            return result;
        }

        public static IReadOnlyList<FluxTelecomFtpFileEntry> ParseFtpFiles(string? html)
        {
            var result = new List<FluxTelecomFtpFileEntry>();
            foreach (Match rowMatch in TableRowRegex.Matches(html ?? string.Empty))
            {
                var cells = TableCellRegex.Matches(rowMatch.Groups[1].Value)
                    .Cast<Match>()
                    .Select(match => DecodeText(match.Groups[1].Value))
                    .ToList();

                if (cells.Count < 11)
                    continue;

                if (string.Equals(cells[1], "Campanha", StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Add(new FluxTelecomFtpFileEntry()
                {
                    Campaign = cells[1],
                    FileName = cells[2],
                    IncludedAtText = cells[3],
                    TotalDeliveries = ParseInt(cells[4]),
                    FirstProposalCount = ParseInt(cells[5]),
                    SecondProposalCount = ParseInt(cells[6]),
                    ThirdProposalCount = ParseInt(cells[7]),
                    FourthProposalCount = ParseInt(cells[8]),
                    FifthProposalCount = ParseInt(cells[9]),
                    SixthProposalCount = ParseInt(cells[10])
                });
            }

            return result;
        }

        private static int? ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = new string(value.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : null;
        }

        private static string DecodeText(string value)
        {
            var decoded = WebUtility.HtmlDecode(HtmlTagRegex.Replace(value ?? string.Empty, " "));
            return MultiWhitespaceRegex.Replace(decoded ?? string.Empty, " ").Trim();
        }
    }
}
