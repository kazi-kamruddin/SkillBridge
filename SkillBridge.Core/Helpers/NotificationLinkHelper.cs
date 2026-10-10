using Microsoft.AspNetCore.Mvc;
using SkillBridge.Models;

namespace SkillBridge.Helpers;

public static class NotificationLinkHelper
{
    public static string Resolve(Notification notification, IUrlHelper url) => notification.Type switch
    {
        "SkillRequest" or "RequestUpdate" when notification.ReferenceId.HasValue =>
            url.Action("RequestDetails", "Profile", new { id = notification.ReferenceId.Value }),
        "Exchange" when notification.ReferenceId.HasValue =>
            url.Action("Details", "Interactions", new { id = notification.ReferenceId.Value }),
        "Feedback" when notification.ReferenceId.HasValue =>
            url.Action("RateInteraction", "Interactions", new { interactionId = notification.ReferenceId.Value }),
        "Skill" when notification.ReferenceId.HasValue =>
            url.Action("Detail", "Explore", new { id = notification.ReferenceId.Value }),
        "Catalog" => url.Action("Skills", "Explore"),
        "Info" when notification.ReferenceId.HasValue &&
            notification.Message == "This exchange request is no longer available." =>
            url.Action("RequestDetails", "Profile", new { id = notification.ReferenceId.Value }),
        "Info" when notification.ReferenceId.HasValue =>
            url.Action("Details", "Interactions", new { id = notification.ReferenceId.Value }),
        _ => url.Action("Index", "Notifications")
    };
}
