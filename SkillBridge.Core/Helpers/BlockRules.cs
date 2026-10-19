using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Models;

namespace SkillBridge.Helpers;

public static class BlockRules
{
    public static bool EitherBlocked(ApplicationDbContext db, string firstId, string secondId) =>
        db.MemberBlocks.Any(block =>
            (block.BlockerId == firstId && block.BlockedId == secondId) ||
            (block.BlockerId == secondId && block.BlockedId == firstId));

    public static async Task<bool> EitherBlockedAsync(ApplicationDbContext db, string firstId, string secondId, CancellationToken cancellationToken = default) =>
        await db.MemberBlocks.AsNoTracking().AnyAsync(block =>
            (block.BlockerId == firstId && block.BlockedId == secondId) ||
            (block.BlockerId == secondId && block.BlockedId == firstId), cancellationToken);
}
