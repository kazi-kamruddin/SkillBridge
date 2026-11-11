using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SkillBridge.Hubs;

namespace SkillBridge.Helpers;

public static class HubNotifications
{
    public static async Task NotifyBadgesAsync(IHubContext<ChatHub> hubContext, ILogger logger = null, params string[] userIds)
    {
        if (hubContext == null || userIds == null || userIds.Length == 0)
            return;

        var targets = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (targets.Length == 0)
            return;

        try
        {
            await hubContext.Clients.Users(targets).SendAsync("badgesChanged");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Could not push SignalR badge refresh to {UserCount} user(s).", targets.Length);
        }
    }
}
