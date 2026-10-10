using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Helpers;
using SkillBridge.Models;
using SkillBridge.Services;

namespace SkillBridge.Controllers;

[Authorize]
public class SavedProfilesController : Controller
{
    private readonly ApplicationDbContext db;
    public SavedProfilesController(ApplicationDbContext db) => this.db = db;

    public async Task<IActionResult> Index()
    {
        var userId = User.Identity.GetUserId();
        var saved = await db.SavedProfiles.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.TargetUserId })
            .ToListAsync();
        var ids = saved.Select(s => s.TargetUserId).ToList();
        var profiles = await db.UserInformations.Where(p => ids.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);
        var blockedIds = await db.MemberBlocks
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId).ToListAsync();
        var myLearning = await db.UserSkills.Where(s => s.UserId == userId && s.Status == "Learning")
            .Select(s => s.SkillId).ToListAsync();
        var skills = await db.UserSkills.Include(s => s.Skill)
            .Where(s => ids.Contains(s.UserId) && s.Status == "Teaching" && myLearning.Contains(s.SkillId))
            .Select(s => new { s.UserId, s.Skill.Name }).ToListAsync();
        var model = saved.Select(s => new SavedProfileItemViewModel
        {
            UserId = s.TargetUserId,
            FullName = profiles.TryGetValue(s.TargetUserId, out var profile) ? profile.FullName : "SkillBridge member",
            Profession = profile?.Profession,
            CanView = profile != null && !profile.IsHidden && !blockedIds.Contains(s.TargetUserId),
            MatchSummary = string.Join(", ", skills.Where(skill => skill.UserId == s.TargetUserId)
                .Select(skill => skill.Name).Distinct())
        }).ToList();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
    public async Task<IActionResult> Save(string id)
    {
        var userId = User.Identity.GetUserId();
        if (string.IsNullOrWhiteSpace(id) || id == userId) return BadRequest();
        if (!await db.UserInformations.AnyAsync(p => p.UserId == id && !p.IsHidden)) return NotFound();
        if (BlockRules.EitherBlocked(db, userId, id)) return StatusCode(403);
        if (!await db.SavedProfiles.AnyAsync(s => s.UserId == userId && s.TargetUserId == id))
        {
            db.SavedProfiles.Add(new SavedProfile { UserId = userId, TargetUserId = id });
            await db.SaveChangesAsync();
        }
        return RedirectToAction("PublicProfile", "Profile", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
    public async Task<IActionResult> Remove(string id, bool fromList = false)
    {
        var userId = User.Identity.GetUserId();
        var saved = await db.SavedProfiles.FindAsync(userId, id);
        if (saved != null)
        {
            db.SavedProfiles.Remove(saved);
            await db.SaveChangesAsync();
        }
        return fromList ? RedirectToAction(nameof(Index)) : RedirectToAction("PublicProfile", "Profile", new { id });
    }
}
