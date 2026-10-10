using System.Text.RegularExpressions;
using SkillBridge.Models;

namespace SkillBridge.Helpers;

public static class MemberNames
{
    private static readonly Regex EmailAddress = new(@"(?<![\w.+-])[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}(?![\w-])", RegexOptions.Compiled);

    public static Dictionary<string, string> For(ApplicationDbContext db, IEnumerable<string> userIds)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        return db.UserInformations.Where(info => ids.Contains(info.UserId))
            .ToDictionary(info => info.UserId, info => info.FullName);
    }

    public static string Get(IReadOnlyDictionary<string, string> names, string userId) =>
        userId != null && names.TryGetValue(userId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name : "SkillBridge member";

    public static string Get(ApplicationDbContext db, string userId) =>
        Get(For(db, new[] { userId }), userId);

    // Earlier notifications included Identity's email-based UserName. Keep those records while hiding the address in UI.
    public static string NotificationText(string message) =>
        EmailAddress.Replace(message ?? "", "a member");
}
