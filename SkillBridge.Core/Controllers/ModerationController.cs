using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Helpers;
using SkillBridge.Models;
using SkillBridge.Services;

namespace SkillBridge.Controllers;

[Authorize]
public class ModerationController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<ModerationController> logger;

    public ModerationController(ApplicationDbContext db, ILogger<ModerationController> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public IActionResult Index()
    {
        if (!IsModerator()) return Forbid();

        var reports = db.CommunityReports.AsNoTracking()
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedAt)
            .ToList();

        var commentIds = reports.Where(r => r.CommentId.HasValue).Select(r => r.CommentId!.Value).Distinct().ToList();
        var commentsById = commentIds.Count > 0
            ? db.CommunityComments.AsNoTracking()
                .Where(c => commentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.PostId, c.Content })
                .ToDictionary(c => c.Id)
            : new();

        var postIds = reports.Where(r => !r.CommentId.HasValue && r.PostId.HasValue)
            .Select(r => r.PostId!.Value)
            .Distinct()
            .ToList();
        var postsById = postIds.Count > 0
            ? db.CommunityPosts.AsNoTracking()
                .Where(p => postIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Title })
                .ToDictionary(p => p.Id)
            : new();

        var model = reports.Select(r =>
        {
            if (r.CommentId.HasValue)
            {
                commentsById.TryGetValue(r.CommentId.Value, out var comment);
                return new ModerationReportViewModel
                {
                    Id = r.Id,
                    PostId = comment?.PostId,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,
                    ContentType = "Comment",
                    ContentPreview = comment?.Content ?? "Removed comment"
                };
            }

            var postTitle = r.PostId.HasValue && postsById.TryGetValue(r.PostId.Value, out var post)
                ? post.Title : null;
            return new ModerationReportViewModel
            {
                Id = r.Id,
                PostId = r.PostId,
                Reason = r.Reason,
                CreatedAt = r.CreatedAt,
                ContentType = "Post",
                ContentPreview = postTitle ?? "Removed post"
            };
        }).ToList();

        var profileReports = db.ProfileReports.AsNoTracking()
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedAt)
            .ToList();

        var reportedUserIds = profileReports.Select(r => r.ReportedUserId).Distinct().ToList();
        var reportedProfiles = reportedUserIds.Count > 0
            ? db.UserInformations.AsNoTracking()
                .Where(info => reportedUserIds.Contains(info.UserId))
                .Select(info => new { info.UserId, info.FullName, info.Bio })
                .ToDictionary(info => info.UserId)
            : new();

        var dashboard = new ModerationDashboardViewModel
        {
            CommunityReports = model,
            ProfileReports = profileReports.Select(r =>
            {
                reportedProfiles.TryGetValue(r.ReportedUserId, out var info);
                return new ProfileReportItemViewModel
                {
                    Id = r.Id,
                    ReportedUserId = r.ReportedUserId,
                    ReportedName = !string.IsNullOrWhiteSpace(info?.FullName) ? info.FullName : "SkillBridge member",
                    ProfileBio = info?.Bio ?? "",
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt
                };
            }).ToList(),
            HiddenProfiles = db.UserInformations.AsNoTracking()
                .Where(info => info.IsHidden)
                .OrderBy(info => info.FullName)
                .Select(info => new HiddenProfileViewModel { UserId = info.UserId, Name = info.FullName })
                .ToList()
        };
        return View(dashboard);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
    public IActionResult Resolve(int id, string decision)
    {
        if (!IsModerator()) return Forbid();
        if (decision != "Dismissed" && decision != "Hidden") return BadRequest();

        var report = db.CommunityReports.FirstOrDefault(r => r.Id == id && r.Status == "Pending");
        if (report == null) return NotFound();

        if (decision == "Hidden")
        {
            if (report.CommentId.HasValue)
            {
                var comment = db.CommunityComments.Find(report.CommentId.Value);
                if (comment != null) comment.IsHidden = true;
                foreach (var related in db.CommunityReports.Where(r =>
                    r.CommentId == report.CommentId && r.Status == "Pending"))
                    related.Status = "Hidden";
            }
            else
            {
                var post = db.CommunityPosts.Find(report.PostId.Value);
                if (post != null) post.IsHidden = true;
                foreach (var related in db.CommunityReports.Where(r =>
                    r.PostId == report.PostId && r.Status == "Pending"))
                    related.Status = "Hidden";
            }
        }
        else
        {
            report.Status = "Dismissed";
        }

        db.SaveChanges();
        logger.LogInformation("Community report {ReportId} resolved as {Decision}", id, decision);
        TempData["ModerationNotice"] = "Report reviewed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
    public IActionResult ResolveProfile(int id, string decision)
    {
        if (!IsModerator()) return Forbid();
        if (decision != "Dismissed" && decision != "Reviewed") return BadRequest();
        var report = db.ProfileReports.FirstOrDefault(r => r.Id == id && r.Status == "Pending");
        if (report == null) return NotFound();
        if (decision == "Reviewed")
        {
            var profile = db.UserInformations.FirstOrDefault(info => info.UserId == report.ReportedUserId);
            if (profile != null) profile.IsHidden = true;
            foreach (var related in db.ProfileReports.Where(r =>
                r.ReportedUserId == report.ReportedUserId && r.Status == "Pending"))
                related.Status = "Reviewed";
        }
        else report.Status = "Dismissed";
        db.SaveChanges();
        logger.LogInformation("Profile report {ReportId} resolved as {Decision}", id, decision);
        TempData["ModerationNotice"] = "Profile report reviewed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
    public IActionResult RestoreProfile(string id)
    {
        if (!IsModerator()) return Forbid();
        var profile = db.UserInformations.FirstOrDefault(info => info.UserId == id && info.IsHidden);
        if (profile == null) return NotFound();
        profile.IsHidden = false;
        db.SaveChanges();
        logger.LogInformation("A hidden profile was restored to discovery");
        TempData["ModerationNotice"] = "Profile restored to discovery.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsModerator()
    {
        var moderatorEmail = Environment.GetEnvironmentVariable("SKILLBRIDGE_MODERATOR_EMAIL");
        if (string.IsNullOrWhiteSpace(moderatorEmail)) return false;
        var userId = User.Identity.GetUserId();
        var email = db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefault();
        return string.Equals(email, moderatorEmail.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
