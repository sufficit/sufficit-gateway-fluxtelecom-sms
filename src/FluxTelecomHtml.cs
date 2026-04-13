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
        private static readonly Regex AnchorRegex = new Regex("<a\\b(?<attributes>[^>]*)>(?<content>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex UserEditLinkRegex = new Regex("javascript:editar\\((\\d+)\\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ReplyDetailLinkRegex = new Regex("javascript:verResposta\\((\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex FormRegex = new Regex("<form\\b(?<attributes>[^>]*)>(?<content>.*?)</form>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex InputRegex = new Regex("<input\\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex TextAreaRegex = new Regex("<textarea\\b(?<attributes>[^>]*)>(?<value>.*?)</textarea>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex SelectRegex = new Regex("<select\\b(?<attributes>[^>]*)>(?<value>.*?)</select>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex OptionRegex = new Regex("<option\\b(?<attributes>[^>]*)>(?<value>.*?)</option>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex AttributeRegex = new Regex("(?<name>[A-Za-z0-9_@.#():-]+)(?:\\s*=\\s*(?:\"(?<double>[^\"]*)\"|'(?<single>[^']*)'|(?<bare>[^\\s>]+)))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        public static IReadOnlyList<FluxTelecomPortalUserEntry> ParsePortalUsers(string? html)
        {
            var result = new List<FluxTelecomPortalUserEntry>();

            foreach (Match rowMatch in TableRowRegex.Matches(html ?? string.Empty))
            {
                var rowHtml = rowMatch.Groups[1].Value;
                var editLinkMatch = UserEditLinkRegex.Match(rowHtml);
                if (!editLinkMatch.Success)
                    continue;

                if (!int.TryParse(editLinkMatch.Groups[1].Value, out var userId))
                    continue;

                var cells = TableCellRegex.Matches(rowHtml)
                    .Cast<Match>()
                    .Select(match => DecodeText(match.Groups[1].Value))
                    .ToList();

                if (cells.Count < 4)
                    continue;

                result.Add(new FluxTelecomPortalUserEntry()
                {
                    UserId = userId,
                    Name = cells[1],
                    Email = cells[2],
                    IsActive = cells[3].IndexOf("Ativo", StringComparison.OrdinalIgnoreCase) >= 0
                });
            }

            return result;
        }

        public static IReadOnlyList<FluxTelecomReplyEntry> ParseReplies(string? html)
        {
            var result = new List<FluxTelecomReplyEntry>();

            foreach (Match rowMatch in TableRowRegex.Matches(html ?? string.Empty))
            {
                var cells = TableCellRegex.Matches(rowMatch.Groups[1].Value)
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value)
                    .ToList();

                if (cells.Count < 10)
                    continue;

                if (string.Equals(DecodeText(cells[0]), "SMS", StringComparison.OrdinalIgnoreCase))
                    continue;

                var detailMatch = ReplyDetailLinkRegex.Match(rowMatch.Groups[1].Value);
                var senderContactHint = TrimPrefixedHint(GetFirstAnchorAttribute(cells[8], "title"), "Nome Contato:");

                result.Add(new FluxTelecomReplyEntry()
                {
                    ReplyId = detailMatch.Success ? ParseInt(detailMatch.Groups[3].Value) : null,
                    CampaignId = detailMatch.Success ? ParseInt(detailMatch.Groups[2].Value) : ParseInt(DecodeText(cells[2])),
                    MessageId = detailMatch.Success ? ParseLong(detailMatch.Groups[1].Value) : ParseLong(DecodeText(cells[5])),
                    CampaignName = GetAnchorText(cells[3], preferTitle: true),
                    CostCenterDescription = GetAnchorText(cells[4], preferTitle: true),
                    SentMessage = GetAnchorText(cells[6]),
                    ReplyText = GetAnchorText(cells[7], preferTitle: true),
                    SenderPhone = GetAnchorText(cells[8]),
                    SenderContactHint = senderContactHint,
                    ReceivedAtText = GetAnchorText(cells[9])
                });
            }

            return result;
        }

        public static IReadOnlyList<KeyValuePair<string, string>> ParseSuccessfulFormFields(string? html, string formId)
        {
            var formContent = ExtractFormContent(html, formId);
            var result = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(formContent))
                return result;

            foreach (Match inputMatch in InputRegex.Matches(formContent))
            {
                var attributes = inputMatch.Groups["attributes"].Value;
                var name = GetAttributeValue(attributes, "name");
                if (string.IsNullOrWhiteSpace(name) || HasBooleanAttribute(attributes, "disabled"))
                    continue;

                var type = GetAttributeValue(attributes, "type") ?? "text";
                if (IsIgnoredInputType(type))
                    continue;

                if ((string.Equals(type, "checkbox", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(type, "radio", StringComparison.OrdinalIgnoreCase))
                    && !HasBooleanAttribute(attributes, "checked"))
                    continue;

                result.Add(new KeyValuePair<string, string>(name, GetAttributeValue(attributes, "value") ?? string.Empty));
            }

            foreach (Match textAreaMatch in TextAreaRegex.Matches(formContent))
            {
                var attributes = textAreaMatch.Groups["attributes"].Value;
                var name = GetAttributeValue(attributes, "name");
                if (string.IsNullOrWhiteSpace(name) || HasBooleanAttribute(attributes, "disabled"))
                    continue;

                result.Add(new KeyValuePair<string, string>(name, WebUtility.HtmlDecode(textAreaMatch.Groups["value"].Value ?? string.Empty)));
            }

            foreach (Match selectMatch in SelectRegex.Matches(formContent))
            {
                var attributes = selectMatch.Groups["attributes"].Value;
                var name = GetAttributeValue(attributes, "name");
                if (string.IsNullOrWhiteSpace(name) || HasBooleanAttribute(attributes, "disabled"))
                    continue;

                string? selectedValue = null;
                string? firstValue = null;

                foreach (Match optionMatch in OptionRegex.Matches(selectMatch.Groups["value"].Value))
                {
                    var optionAttributes = optionMatch.Groups["attributes"].Value;
                    var optionValue = GetAttributeValue(optionAttributes, "value") ?? string.Empty;

                    if (firstValue == null)
                        firstValue = optionValue;

                    if (HasBooleanAttribute(optionAttributes, "selected"))
                    {
                        selectedValue = optionValue;
                        break;
                    }
                }

                result.Add(new KeyValuePair<string, string>(name, selectedValue ?? firstValue ?? string.Empty));
            }

            return result;
        }

        public static string ExtractText(string? html)
            => DecodeText(html ?? string.Empty);

        public static string? ParseSingleTableCellText(string? html)
        {
            foreach (Match rowMatch in TableRowRegex.Matches(html ?? string.Empty))
            {
                var cells = TableCellRegex.Matches(rowMatch.Groups[1].Value)
                    .Cast<Match>()
                    .Select(match => DecodeText(match.Groups[1].Value))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                if (cells.Count == 1)
                    return cells[0];
            }

            return null;
        }

        private static int? ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = new string(value.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : null;
        }

        private static long? ParseLong(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = new string(value.Where(char.IsDigit).ToArray());
            return long.TryParse(digits, out var number) ? number : null;
        }

        private static string GetAnchorText(string cellHtml, bool preferTitle = false)
        {
            var title = preferTitle ? GetFirstAnchorAttribute(cellHtml, "title") : null;
            if (!string.IsNullOrWhiteSpace(title))
                return DecodeText(title);

            var anchorMatch = AnchorRegex.Match(cellHtml ?? string.Empty);
            if (anchorMatch.Success)
                return DecodeText(anchorMatch.Groups["content"].Value);

            return DecodeText(cellHtml ?? string.Empty);
        }

        private static string DecodeText(string value)
        {
            var decoded = WebUtility.HtmlDecode(HtmlTagRegex.Replace(value ?? string.Empty, " "));
            return MultiWhitespaceRegex.Replace(decoded ?? string.Empty, " ").Trim();
        }

        private static string? ExtractFormContent(string? html, string formId)
        {
            foreach (Match formMatch in FormRegex.Matches(html ?? string.Empty))
            {
                var attributes = formMatch.Groups["attributes"].Value;
                var id = GetAttributeValue(attributes, "id");
                if (string.Equals(id, formId, StringComparison.OrdinalIgnoreCase))
                    return formMatch.Groups["content"].Value;
            }

            return null;
        }

        private static string? GetAttributeValue(string attributes, string attributeName)
        {
            foreach (Match attributeMatch in AttributeRegex.Matches(attributes ?? string.Empty))
            {
                var name = attributeMatch.Groups["name"].Value;
                if (!string.Equals(name, attributeName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (attributeMatch.Groups["double"].Success)
                    return WebUtility.HtmlDecode(attributeMatch.Groups["double"].Value);

                if (attributeMatch.Groups["single"].Success)
                    return WebUtility.HtmlDecode(attributeMatch.Groups["single"].Value);

                if (attributeMatch.Groups["bare"].Success)
                    return WebUtility.HtmlDecode(attributeMatch.Groups["bare"].Value);

                return string.Empty;
            }

            return null;
        }

        private static string? GetFirstAnchorAttribute(string html, string attributeName)
        {
            var anchorMatch = AnchorRegex.Match(html ?? string.Empty);
            if (!anchorMatch.Success)
                return null;

            return GetAttributeValue(anchorMatch.Groups["attributes"].Value, attributeName);
        }

        private static string? TrimPrefixedHint(string? value, string prefix)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(prefix.Length).Trim();

            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private static bool HasBooleanAttribute(string attributes, string attributeName)
            => Regex.IsMatch(
                attributes ?? string.Empty,
                $"\\b{Regex.Escape(attributeName)}(?:\\s*=\\s*(?:\"[^\"]*\"|'[^']*'|[^\\s>]+))?",
                RegexOptions.IgnoreCase);

        private static bool IsIgnoredInputType(string inputType)
            => string.Equals(inputType, "button", StringComparison.OrdinalIgnoreCase)
            || string.Equals(inputType, "submit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(inputType, "reset", StringComparison.OrdinalIgnoreCase)
            || string.Equals(inputType, "file", StringComparison.OrdinalIgnoreCase)
            || string.Equals(inputType, "image", StringComparison.OrdinalIgnoreCase);
    }
}
