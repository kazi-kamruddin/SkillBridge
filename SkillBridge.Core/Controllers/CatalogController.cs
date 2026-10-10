using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Helpers;
using SkillBridge.Models;
using SkillBridge.Services;

namespace SkillBridge.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly ApplicationDbContext db;
    public CatalogController(ApplicationDbContext db) => this.db = db;

    public IActionResult Suggestions()
    {
        if (!IsCatalogEditor()) return Forbid();
        var suggestions = db.SkillSuggestions.Where(s => s.Status == "Pending")
            .OrderBy(s => s.CreatedAt).ToList();
        return View(suggestions);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
    public IActionResult Review(int id, string decision, List<string> stages)
    {
        if (!IsCatalogEditor()) return Forbid();
        var suggestion = db.SkillSuggestions.FirstOrDefault(s => s.Id == id && s.Status == "Pending");
        if (suggestion == null) return NotFound();
        if (decision != "Add" && decision != "Decline") return BadRequest();
        if (decision == "Add")
        {
            stages ??= new();
            stages = stages.Select(s => (s ?? "").Trim()).ToList();
            if (stages.Count != 7 || stages.Any(s => s.Length is < 2 or > 200) ||
                stages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 7)
                return BadRequest("Provide seven distinct milestone titles of 2 to 200 characters.");
        }

        using var transaction = db.Database.BeginTransaction();
        int? newSkillId = null;
        if (decision == "Add")
        {
            if (db.Skills.Any(s => s.Name.ToLower() == suggestion.Name.ToLower()))
                return Conflict("This skill already exists. Decline the duplicate suggestion.");
            var category = db.SkillCategories.FirstOrDefault(c => c.Name.ToLower() == suggestion.CategoryName.ToLower());
            if (category == null)
            {
                category = new SkillCategory { Name = suggestion.CategoryName,
                    Description = "Skills suggested by members" };
                db.SkillCategories.Add(category);
                db.SaveChanges();
            }
            var skill = new Skill { Name = suggestion.Name, SkillCategoryId = category.Id };
            db.Skills.Add(skill);
            db.SaveChanges();
            newSkillId = skill.Id;
            for (var i = 0; i < stages.Count; i++)
                db.SkillStages.Add(new SkillStage { SkillId = skill.Id, StageNumber = i + 1, Description = stages[i] });
            db.Communities.Add(new Community { SkillId = skill.Id, Name = skill.Name,
                Description = "Community for skill " + skill.Name });
        }
        suggestion.Status = decision == "Add" ? "Added" : "Declined";
        db.Notifications.Add(new Notification
        {
            UserId = suggestion.UserId,
            Type = newSkillId.HasValue ? "Skill" : "Catalog",
            ReferenceId = newSkillId,
            Message = decision == "Add" ? $"Your suggestion, {suggestion.Name}, was added to the skills directory."
                : $"Your suggestion, {suggestion.Name}, was reviewed and not added."
        });
        db.SaveChanges();
        transaction.Commit();
        TempData["CatalogNotice"] = "Suggestion reviewed.";
        return RedirectToAction(nameof(Suggestions));
    }

    private bool IsCatalogEditor()
    {
        var editorEmail = Environment.GetEnvironmentVariable("SKILLBRIDGE_MODERATOR_EMAIL");
        if (string.IsNullOrWhiteSpace(editorEmail)) return false;
        var userId = User.Identity.GetUserId();
        var email = db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefault();
        return string.Equals(email, editorEmail.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
