using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Models;
using SkillBridge.Services;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class CompleteProfileController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly CloudinaryImageService images;

        public CompleteProfileController(ApplicationDbContext db, CloudinaryImageService images)
        {
            this.db = db;
            this.images = images;
        }

        ////////////////////////////////////////////////////////////////////////////
        // GET: /CompleteProfile/

        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            if (await db.UserInformations.AsNoTracking().AnyAsync(ui => ui.UserId == userId))
                return RedirectToAction("Index", "Profile");

            var skillData = await db.SkillCategories
                .AsNoTracking()
                .Include("Skills.SkillStages")
                .ToListAsync();

            var model = new CompleteProfileViewModel
            {
                FullName = await ResolveFallbackDisplayNameAsync(userId),
                AllSkillCategories = skillData
            };

            return View(model);
        }

        ////////////////////////////////////////////////////////////////////////////
        // POST: /CompleteProfile/

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<ActionResult> Index(CompleteProfileViewModel model)
        {
            var userId = User.Identity.GetUserId();
            if (await db.UserInformations.AsNoTracking().AnyAsync(ui => ui.UserId == userId))
                return RedirectToAction("Index", "Profile");

            if (string.IsNullOrWhiteSpace(model.FullName))
                ModelState.AddModelError(nameof(model.FullName), "Full name is required.");

            var learningIds = model.SkillsToLearn ?? new List<int>();
            var teachingSkills = model.SkillsIKnow ?? new List<CompleteProfileViewModel.UserKnownSkill>();
            var validSkillIds = new HashSet<int>(await db.Skills.AsNoTracking().Select(s => s.Id).ToListAsync());
            var maxStageBySkill = (await db.SkillStages.AsNoTracking().ToListAsync())
                .GroupBy(s => s.SkillId)
                .ToDictionary(g => g.Key, g => g.Max(s => s.StageNumber));

            var imageError = CloudinaryImageService.Validate(model.ProfileImage, 2 * 1024 * 1024);
            if (imageError != null) ModelState.AddModelError(nameof(model.ProfileImage), imageError);
            if (learningIds.Any(id => !validSkillIds.Contains(id)) ||
                teachingSkills.Any(s => !validSkillIds.Contains(s.SkillId) ||
                    !maxStageBySkill.ContainsKey(s.SkillId) ||
                    s.KnownUpToStage < 1 || s.KnownUpToStage > maxStageBySkill[s.SkillId]))
                ModelState.AddModelError("", "Choose valid skills and stages.");
            if (teachingSkills.Select(s => s.SkillId).Distinct().Count() != teachingSkills.Count)
                ModelState.AddModelError("", "Choose each teaching skill only once.");

            if (!ModelState.IsValid)
            {
                model.AllSkillCategories = await db.SkillCategories
                    .AsNoTracking()
                    .Include("Skills.SkillStages")
                    .ToListAsync();
                return View(model);
            }

            UploadedImage uploaded = null;
            if (model.ProfileImage?.Length > 0)
            {
                try
                {
                    uploaded = await images.UploadAsync(model.ProfileImage, "skillbridge/avatars", false);
                }
                catch
                {
                    ModelState.AddModelError(nameof(model.ProfileImage), "The image could not be uploaded. Please try again.");
                    model.AllSkillCategories = await db.SkillCategories
                        .AsNoTracking()
                        .Include("Skills.SkillStages")
                        .ToListAsync();
                    return View(model);
                }
            }

            var userInfo = new UserInformation
            {
                UserId = userId,
                FullName = model.FullName.Trim(),
                Age = model.Age,
                Profession = model.Profession?.Trim(),
                Location = model.Location?.Trim(),
                Bio = model.Bio?.Trim(),
                IsPublic = model.IsPublic,
                MeetingFormat = "Either",
                ProfileImageUrl = uploaded?.Url,
                ProfileImagePublicId = uploaded?.PublicId
            };
            db.UserInformations.Add(userInfo);

            if (!await db.UserRatings.AsNoTracking().AnyAsync(r => r.UserId == userId))
                db.UserRatings.Add(new UserRating { UserId = userId });

            if (model.SkillsToLearn != null && model.SkillsToLearn.Any())
            {
                foreach (var skillId in model.SkillsToLearn.Distinct())
                {
                    if (validSkillIds.Contains(skillId))
                    {
                        db.UserSkills.Add(new UserSkill
                        {
                            UserId = userId,
                            SkillId = skillId,
                            Status = "Learning",
                            KnownUpToStage = 0
                        });
                    }
                }
            }

            if (model.SkillsIKnow != null && model.SkillsIKnow.Any())
            {
                foreach (var skillKnown in model.SkillsIKnow)
                {
                    if (skillKnown.SkillId > 0 && validSkillIds.Contains(skillKnown.SkillId))
                    {
                        db.UserSkills.Add(new UserSkill
                        {
                            UserId = userId,
                            SkillId = skillKnown.SkillId,
                            Status = "Teaching",
                            KnownUpToStage = skillKnown.KnownUpToStage
                        });
                    }
                }
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await db.UserInformations.AsNoTracking().AnyAsync(ui => ui.UserId == userId))
                    return RedirectToAction("Index", "Home");
                throw;
            }

            return RedirectToAction("Index", "Home");
        }

        ////////////////////////////////////////////////////////////////////////////
        // POST: /CompleteProfile/Skip

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> Skip()
        {
            var userId = User.Identity.GetUserId();
            if (await db.UserInformations.AsNoTracking().AnyAsync(ui => ui.UserId == userId))
                return RedirectToAction("Index", "Home");

            var fallbackName = await ResolveFallbackDisplayNameAsync(userId);
            db.UserInformations.Add(new UserInformation
            {
                UserId = userId,
                FullName = fallbackName,
                IsPublic = false,
                MeetingFormat = "Either"
            });

            if (!await db.UserRatings.AsNoTracking().AnyAsync(r => r.UserId == userId))
                db.UserRatings.Add(new UserRating { UserId = userId });

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await db.UserInformations.AsNoTracking().AnyAsync(ui => ui.UserId == userId))
                    return RedirectToAction("Index", "Home");
                throw;
            }

            TempData["AccountNotice"] = "Welcome to SkillBridge! You can browse freely and update your name or skills from your profile whenever you are ready.";
            return RedirectToAction("Index", "Home");
        }

        private async Task<string> ResolveFallbackDisplayNameAsync(string userId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                email = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();
            }

            foreach (var claimType in new[] { ClaimTypes.GivenName, ClaimTypes.Name })
            {
                var claimValue = User.FindFirstValue(claimType)?.Trim();
                if (!string.IsNullOrWhiteSpace(claimValue) &&
                    !claimValue.Contains('@') &&
                    !string.Equals(claimValue, email, StringComparison.OrdinalIgnoreCase))
                {
                    return claimValue.Length > 100 ? claimValue[..100] : claimValue;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var atIndex = email.IndexOf('@');
                var localPart = atIndex > 0 ? email[..atIndex] : email;
                var tokens = Regex.Split(localPart, @"[^A-Za-z]+")
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(t.ToLowerInvariant()))
                    .ToList();

                if (tokens.Count > 0)
                {
                    var derived = string.Join(" ", tokens);
                    if (derived.Length >= 2)
                        return derived.Length > 100 ? derived[..100] : derived;
                }
            }

            return "SkillBridge Member";
        }
    }
}
