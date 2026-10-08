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

    public ModerationController(ApplicationDbContext db) => this.db = db;

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
        return View(model);
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
        TempData["ModerationNotice"] = "Report reviewed.";
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
