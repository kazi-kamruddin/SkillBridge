using System.Security.Claims;
using System.Security.Principal;

namespace SkillBridge.Helpers;

public static class ClaimsIdentityExtensions
{
    public static string GetUserId(this IIdentity identity) =>
        (identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
