using SkillBridge.Models;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SkillBridge.Services;
using SkillBridge.Helpers;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class ExploreController : Controller
    {
        private readonly ApplicationDbContext db;

        public ExploreController(ApplicationDbContext db) => this.db = db;

        ////////////////////////////////////////////////////////////////////////////
        [AllowAnonymous]
        public async Task<ActionResult> Index(string skillFilter = "", int stageFilter = 0, string locationFilter = "")
        {
            if (!User.Identity.IsAuthenticated)
                return await Skills("");

            var currentUserId = User.Identity.GetUserId();
            if (!await db.UserInformations.AsNoTracking().AnyAsync(info => info.UserId == currentUserId))
                return RedirectToAction("Index", "CompleteProfile");

            var currentUserSkills = await db.UserSkills
                .AsNoTracking()
                .Include(us => us.Skill.SkillStages)
                .Where(us => us.UserId == currentUserId)
                .ToListAsync();

            var teachingSkillsIds = currentUserSkills
                .Where(us => us.Status == "Teaching" && us.KnownUpToStage > 0)
                .Select(us => us.SkillId)
                .ToList();

            var learningSkills = currentUserSkills
                .Where(us => us.Status == "Learning")
                .Select(us => new { us.SkillId, us.Skill.Name })
                .ToList();

            // Cap candidate pool to avoid unbounded memory exhaustion while preserving high match quality
            var otherUsers = await db.UserInformations
                .AsNoTracking()
                .Where(info => info.UserId != currentUserId && !info.IsHidden)
                .Where(info => !db.MemberBlocks.Any(block =>
                    (block.BlockerId == currentUserId && block.BlockedId == info.UserId) ||
                    (block.BlockedId == currentUserId && block.BlockerId == info.UserId)))
                .Take(200)
                .Select(info => new { Id = info.UserId, Info = info })
                .ToListAsync();

            var otherIds = otherUsers.Select(user => user.Id).ToList();
            var skillsByUserRaw = await db.UserSkills
                .AsNoTracking()
                .Include(us => us.Skill.SkillStages)
                .Where(us => otherIds.Contains(us.UserId))
                .ToListAsync();
            var skillsByUser = skillsByUserRaw.ToLookup(us => us.UserId);

            var ratingsByUser = await db.UserRatings
                .AsNoTracking()
                .Where(rating => otherIds.Contains(rating.UserId))
                .ToDictionaryAsync(rating => rating.UserId);

            var bestMatches = new List<PublicProfileViewModel>();
            var partialMatches = new List<PublicProfileViewModel>();

            foreach (var user in otherUsers)
            {
                var userInfo = user.Info;
                var userSkills = skillsByUser[user.Id].ToList();

                var userTeachingSkills = userSkills
                    .Where(us => us.Status == "Teaching" && us.KnownUpToStage > 0)
                    .ToList();

                var userLearningSkills = userSkills
                    .Where(us => us.Status == "Learning")
                    .ToList();

                bool isBestMatch = userLearningSkills.Any(l => teachingSkillsIds.Contains(l.SkillId) &&
                    userTeachingSkills.Any(t => t.SkillId != l.SkillId && t.KnownUpToStage > 0 &&
                        learningSkills.Any(wanted => wanted.SkillId == t.SkillId)));

                bool isPartialMatch = !isBestMatch && userTeachingSkills.Any(t => t.KnownUpToStage > 0 &&
                    learningSkills.Any(wanted => wanted.SkillId == t.SkillId));

                ratingsByUser.TryGetValue(user.Id, out var userRating);
                double averageRating = (userRating != null && userRating.RatingsReceived > 0)
                    ? (double)userRating.AccumulatedRating / userRating.RatingsReceived
                    : 0;

                var profileVm = new PublicProfileViewModel
                {
                    UserId = user.Id,
                    FullName = userInfo?.FullName ?? "",
                    Profession = userInfo?.Profession ?? "",
                    Location = userInfo?.Location ?? "",
                    Bio = userInfo?.Bio ?? "",
                    AvailabilityNotes = userInfo?.AvailabilityNotes,
                    AvailableDaysMask = userInfo?.AvailableDaysMask ?? 0,
                    TimeZoneId = userInfo?.TimeZoneId,
                    MeetingFormat = userInfo?.MeetingFormat ?? "Either",
                    AverageRating = averageRating,
                    CanKnock = currentUserSkills.Any(s => s.Status == "Teaching" && s.KnownUpToStage > 0) &&
                        learningSkills.Any() && userTeachingSkills.Any() && userLearningSkills.Any(),
                    ProfileImageUrl = ProfileImageHelper.GetProfileImage(userInfo?.ProfileImageUrl, userInfo?.FullName),
                    YouCanLearn = string.Join(", ", userTeachingSkills
                        .Where(us => learningSkills.Any(ls => ls.SkillId == us.SkillId))
                        .Select(us => $"{us.Skill.Name} (stage {us.KnownUpToStage ?? 0}/{us.Skill.SkillStages.Count})")),
                    TheyCanLearn = string.Join(", ", currentUserSkills
                        .Where(us => us.Status == "Teaching" && userLearningSkills.Any(ls => ls.SkillId == us.SkillId))
                        .Select(us => $"{us.Skill.Name} (stage {us.KnownUpToStage ?? 0}/{us.Skill.SkillStages.Count})")),

                    SkillsToTeach = userTeachingSkills
                        .Select(us => new SkillViewModel
                        {
                            SkillId = us.SkillId,
                            SkillName = us.Skill.Name,
                            Stage = us.KnownUpToStage ?? 1
                        }).ToList(),
                    SkillsToLearn = userLearningSkills
                        .Select(us => new SkillViewModel
                        {
                            SkillId = us.SkillId,
                            SkillName = us.Skill.Name,
                            Stage = us.KnownUpToStage ?? 1
                        }).ToList()
                };

                if ((!string.IsNullOrEmpty(skillFilter) || stageFilter > 0) &&
                    !profileVm.SkillsToTeach.Any(s =>
                        (string.IsNullOrEmpty(skillFilter) || s.SkillName == skillFilter) && s.Stage >= stageFilter))
                    continue;

                if (!string.IsNullOrEmpty(locationFilter) &&
                    !string.Equals(profileVm.Location, locationFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (isBestMatch) bestMatches.Add(profileVm);
                else if (isPartialMatch || (currentUserSkills.Count == 0 && userTeachingSkills.Count > 0)) partialMatches.Add(profileVm);
            }

            bestMatches = bestMatches
                .OrderByDescending(b => b.AverageRating)
                .ThenByDescending(b => b.SkillsToTeach.Count)
                .ToList();

            partialMatches = partialMatches
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.SkillsToTeach.Count)
                .ToList();

            var maxStage = await db.SkillStages.AsNoTracking().MaxAsync(s => (int?)s.StageNumber);

            var model = new ExploreViewModel
            {
                MaxCatalogStage = maxStage ?? 7,
                TimeZones = bestMatches.Concat(partialMatches)
                    .Select(m => m.TimeZoneId).Where(zone => !string.IsNullOrWhiteSpace(zone))
                    .Distinct().OrderBy(zone => zone).ToList(),
                Locations = bestMatches.Concat(partialMatches)
                    .Select(m => m.Location).Where(location => !string.IsNullOrWhiteSpace(location))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(location => location).ToList(),
                BestMatches = bestMatches,
                PartialMatches = partialMatches,
                LearningSkillNames = learningSkills.Select(s => s.Name).Distinct().ToList()
            };

            return View(model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> Skills(string q = "")
        {
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q[..100];
            var currentUserId = User.Identity.GetUserId();
            var publicTeachers = await (from userSkill in db.UserSkills.AsNoTracking()
                                  join info in db.UserInformations.AsNoTracking() on userSkill.UserId equals info.UserId
                                  where userSkill.Status == "Teaching" && info.IsPublic && !info.IsHidden
                                  select new { userSkill.SkillId, info.UserId, info.FullName }).ToListAsync();
            var skills = await db.Skills.AsNoTracking()
                .Include(s => s.SkillCategory)
                .Include(s => s.SkillStages)
                .ToListAsync();
            var catalogEditorEmail = Environment.GetEnvironmentVariable("SKILLBRIDGE_MODERATOR_EMAIL");
            var canReview = currentUserId != null && !string.IsNullOrWhiteSpace(catalogEditorEmail) &&
                await db.Users.AsNoTracking().AnyAsync(u => u.Id == currentUserId && u.Email.ToLower() == catalogEditorEmail.Trim().ToLower());

            List<SkillSuggestion> mySuggestions = currentUserId == null ? new() : await db.SkillSuggestions
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();

            var model = new GuestExploreViewModel
            {
                Query = q,
                CanReviewSuggestions = canReview,
                MySuggestions = mySuggestions,
                Skills = SkillSearch.Rank(skills, q).Select(skill => new GuestSkillViewModel
                {
                    SkillId = skill.Id,
                    SkillName = skill.Name,
                    CategoryName = skill.SkillCategory.Name,
                    Teachers = publicTeachers.Where(t => t.SkillId == skill.Id)
                        .Select(t => new GuestTeacherViewModel { UserId = t.UserId, FullName = t.FullName }).ToList()
                }).ToList()
            };
            return View("GuestIndex", model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> Detail(int id)
        {
            var skill = await db.Skills.AsNoTracking()
                .Include(s => s.SkillCategory)
                .Include(s => s.SkillStages)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (skill == null) return NotFound();
            var userId = User.Identity.GetUserId();
            var teachersRaw = await (from userSkill in db.UserSkills.AsNoTracking()
                            join info in db.UserInformations.AsNoTracking() on userSkill.UserId equals info.UserId
                            where userSkill.SkillId == id && userSkill.Status == "Teaching" &&
                                userSkill.KnownUpToStage > 0 && !info.IsHidden &&
                                (info.IsPublic || userId != null) && info.UserId != userId
                            select new { info.UserId, info.FullName }).ToListAsync();

            var teachers = teachersRaw
                .Where(t => userId == null || !BlockRules.EitherBlocked(db, userId, t.UserId))
                .Select(t => new GuestTeacherViewModel { UserId = t.UserId, FullName = t.FullName }).ToList();

            var communityId = await db.Communities.AsNoTracking()
                .Where(c => c.SkillId == id)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

            return View(new SkillDetailViewModel
            {
                SkillId = skill.Id,
                SkillName = skill.Name,
                Description = skill.Description,
                CategoryName = skill.SkillCategory?.Name,
                Stages = skill.SkillStages.OrderBy(s => s.StageNumber).ToList(),
                Teachers = teachers,
                CommunityId = communityId
            });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> SuggestSkill(string name, string categoryName, string reason)
        {
            var userId = User.Identity.GetUserId();
            name = (name ?? "").Trim();
            categoryName = (categoryName ?? "").Trim();
            reason = (reason ?? "").Trim();
            if (name.Length is < 2 or > 100 || categoryName.Length is < 2 or > 100 ||
                reason.Length is < 10 or > 500)
            {
                TempData["SkillNotice"] = "Add a skill name, category and short reason within the field limits.";
                return RedirectToAction(nameof(Skills));
            }
            if (await db.Skills.AsNoTracking().AnyAsync(s => s.Name.ToLower() == name.ToLower()))
            {
                TempData["SkillNotice"] = "That skill is already in the directory.";
                return RedirectToAction(nameof(Skills));
            }
            if (await db.SkillSuggestions.AsNoTracking().AnyAsync(s => s.UserId == userId && s.Status == "Pending" &&
                s.Name.ToLower() == name.ToLower()))
            {
                TempData["SkillNotice"] = "You already suggested that skill.";
                return RedirectToAction(nameof(Skills));
            }
            db.SkillSuggestions.Add(new SkillSuggestion
            {
                UserId = userId, Name = name, CategoryName = categoryName, Reason = reason
            });
            await db.SaveChangesAsync();
            TempData["SkillNotice"] = "Thanks. Your suggestion is in review.";
            return RedirectToAction(nameof(Skills));
        }
    }
}
