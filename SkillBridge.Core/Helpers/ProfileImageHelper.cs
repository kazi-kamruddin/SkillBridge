using System.Net;

namespace SkillBridge.Helpers;

public static class ProfileImageHelper
{
    public static string GetProfileImage(string imageUrl, string fullName)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
            return imageUrl.Replace("/image/upload/", "/image/upload/c_fill,g_auto,w_320,h_320,f_auto,q_auto/", StringComparison.Ordinal);
        var first = string.IsNullOrWhiteSpace(fullName) ? "?" : fullName.Trim()[0].ToString().ToUpperInvariant();
        var letter = WebUtility.HtmlEncode(first);
        var colors = new[] { ("#286e68", "#5ba69b"), ("#644a89", "#a17bbc"),
            ("#a14755", "#de897b"), ("#315e91", "#79a1c7") };
        var index = (int)(unchecked((uint)(fullName ?? "").Aggregate(17, (hash, c) => hash * 31 + c)) % colors.Length);
        var color = colors[index];
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='200' height='200' viewBox='0 0 200 200'><defs><linearGradient id='g' x2='1' y2='1'><stop stop-color='{color.Item1}'/><stop offset='1' stop-color='{color.Item2}'/></linearGradient></defs><rect width='200' height='200' rx='100' fill='url(#g)'/><text x='100' y='110' fill='white' text-anchor='middle' dominant-baseline='middle' font-family='Arial,sans-serif' font-weight='700' font-size='90'>{letter}</text></svg>";
        return "data:image/svg+xml," + Uri.EscapeDataString(svg);
    }
}
