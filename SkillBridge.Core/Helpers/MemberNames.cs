using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Models;

namespace SkillBridge.Helpers;

public static class MemberNames
{
    private static readonly Regex EmailAddress = new(@"(?<![\w.+-])[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}(?![\w-])", RegexOptions.Compiled);

    public static Dictionary<string, string> For(ApplicationDbContext db, IEnumerable<string> userIds)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        return db.UserInformations.AsNoTracking().Where(info => ids.Contains(info.UserId))
            .ToDictionary(info => info.UserId, info => info.FullName);
    }

    public static async Task<Dictionary<string, string>> ForAsync(ApplicationDbContext db, IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        return await db.UserInformations.AsNoTracking().Where(info => ids.Contains(info.UserId))
            .ToDictionaryAsync(info => info.UserId, info => info.FullName, cancellationToken);
    }

    public static string Get(IReadOnlyDictionary<string, string> names, string userId) =>
        userId != null && names.TryGetValue(userId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name : "SkillBridge member";

    public static string Get(ApplicationDbContext db, string userId) =>
        Get(For(db, new[] { userId }), userId);

    public static async Task<string> GetAsync(ApplicationDbContext db, string userId, CancellationToken cancellationToken = default) =>
        Get(await ForAsync(db, new[] { userId }, cancellationToken), userId);

    // Earlier notifications included Identity's email-based UserName. Keep those records while hiding the address in UI.
    public static string NotificationText(string message) =>
        EmailAddress.Replace(message ?? "", "a member");
}
