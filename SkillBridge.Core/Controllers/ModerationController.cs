using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Helpers;
using SkillBridge.Models;

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

        var reports = db.CommunityReports.Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedAt).ToList();
        var model = reports.Select(r => new ModerationReportViewModel
        {
            Id = r.Id,
            PostId = r.CommentId.HasValue
                ? db.CommunityComments.Where(c => c.Id == r.CommentId.Value)
                    .Select(c => (int?)c.PostId).FirstOrDefault()
                : r.PostId,
            Reason = r.Reason,
            CreatedAt = r.CreatedAt,
            ContentType = r.CommentId.HasValue ? "Comment" : "Post",
            ContentPreview = r.CommentId.HasValue
                ? db.CommunityComments.Where(c => c.Id == r.CommentId.Value)
                    .Select(c => c.Content).FirstOrDefault() ?? "Removed comment"
                : db.CommunityPosts.Where(p => p.Id == r.PostId.Value)
                    .Select(p => p.Title).FirstOrDefault() ?? "Removed post"
        }).ToList();
        var profileReports = db.ProfileReports.Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedAt).ToList();
        var dashboard = new ModerationDashboardViewModel
        {
            CommunityReports = model,
            ProfileReports = profileReports.Select(r => new ProfileReportItemViewModel
            {
                Id = r.Id,
                ReportedUserId = r.ReportedUserId,
                ReportedName = db.UserInformations.Where(info => info.UserId == r.ReportedUserId)
                    .Select(info => info.FullName).FirstOrDefault() ?? "SkillBridge member",
                ProfileBio = db.UserInformations.Where(info => info.UserId == r.ReportedUserId)
                    .Select(info => info.Bio).FirstOrDefault() ?? "",
                Reason = r.Reason,
                CreatedAt = r.CreatedAt
            }).ToList(),
            HiddenProfiles = db.UserInformations.Where(info => info.IsHidden)
                .OrderBy(info => info.FullName)
                .Select(info => new HiddenProfileViewModel { UserId = info.UserId, Name = info.FullName })
                .ToList()
        };
        return View(dashboard);
    }

    [HttpPost, ValidateAntiForgeryToken]
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

    [HttpPost, ValidateAntiForgeryToken]
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

    [HttpPost, ValidateAntiForgeryToken]
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
        var email = db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefault();
        return string.Equals(email, moderatorEmail.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
