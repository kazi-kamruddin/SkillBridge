using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace SkillBridge.Helpers;

public static class PostTextFormatter
{
    private static readonly Regex Bold = new(@"\*\*([^*\r\n]+)\*\*", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Italic = new(@"(?<!\*)\*([^*\r\n]+)\*(?!\*)", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static IHtmlContent Render(string content)
    {
        if (string.IsNullOrEmpty(content)) return HtmlString.Empty;
        var html = new StringBuilder();
        foreach (var line in (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var heading = line.StartsWith("## ", StringComparison.Ordinal);
            var safe = HtmlEncoder.Default.Encode(heading ? line[3..] : line);
            safe = Bold.Replace(safe, "<strong>$1</strong>");
            safe = Italic.Replace(safe, "<em>$1</em>");
            html.Append(heading ? "<h3>" : "<p>");
            html.Append(safe.Length == 0 ? "<br>" : safe);
            html.Append(heading ? "</h3>" : "</p>");
        }
        return new HtmlString(html.ToString());
    }
}
