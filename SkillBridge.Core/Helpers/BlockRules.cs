using SkillBridge.Models;

namespace SkillBridge.Helpers;

public static class BlockRules
{
    public static bool EitherBlocked(ApplicationDbContext db, string firstId, string secondId) =>
        db.MemberBlocks.Any(block =>
            (block.BlockerId == firstId && block.BlockedId == secondId) ||
            (block.BlockerId == secondId && block.BlockedId == firstId));
}
