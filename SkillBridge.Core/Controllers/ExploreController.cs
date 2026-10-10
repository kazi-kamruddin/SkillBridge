
using SkillBridge.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
        public ActionResult Index(string skillFilter = "", int stageFilter = 0, string locationFilter = "")
        {
            if (!User.Identity.IsAuthenticated)
                return Skills("");

            var currentUserId = User.Identity.GetUserId();
            if (!db.UserInformations.Any(info => info.UserId == currentUserId))
                return RedirectToAction("Index", "CompleteProfile");

            var currentUserSkills = db.UserSkills
                .Include(us => us.Skill.SkillStages)
                .Where(us => us.UserId == currentUserId)
                .ToList();

            var teachingSkillsIds = currentUserSkills
                .Where(us => us.Status == "Teaching")
                .Select(us => us.SkillId)
                .ToList();

            var learningSkills = currentUserSkills
                .Where(us => us.Status == "Learning")
                .Select(us => new { us.SkillId, us.Skill.Name })
                .ToList();


            var otherUsers = db.Users
                .Where(u => u.Id != currentUserId)
                .ToList();

            var bestMatches = new List<PublicProfileViewModel>();
            var partialMatches = new List<PublicProfileViewModel>();

            foreach (var user in otherUsers)
            {
                if (BlockRules.EitherBlocked(db, currentUserId, user.Id)) continue;
                var userInfo = db.UserInformations.FirstOrDefault(ui => ui.UserId == user.Id);
                if (userInfo == null || userInfo.IsHidden) continue;

                var userSkills = db.UserSkills
                    .Include(us => us.Skill.SkillCategory)
                    .Include(us => us.Skill.SkillStages)
                    .Where(us => us.UserId == user.Id)
                    .ToList();

                var userTeachingSkills = userSkills
                    .Where(us => us.Status == "Teaching")
                    .ToList();

                var userLearningSkills = userSkills
                    .Where(us => us.Status == "Learning")
                    .ToList();

                bool isBestMatch = userLearningSkills.Any(l => teachingSkillsIds.Contains(l.SkillId) &&
                    userTeachingSkills.Any(t => t.SkillId != l.SkillId && t.KnownUpToStage > 0 &&
                        learningSkills.Any(wanted => wanted.SkillId == t.SkillId)));

                bool isPartialMatch = !isBestMatch && userTeachingSkills.Any(t => t.KnownUpToStage > 0 &&
                    learningSkills.Any(wanted => wanted.SkillId == t.SkillId));

                var userRating = db.UserRatings.FirstOrDefault(ur => ur.UserId == user.Id);
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



                if (!string.IsNullOrEmpty(skillFilter) &&
                    !profileVm.SkillsToTeach.Any(s => s.SkillName == skillFilter))
                    continue;

                if (stageFilter > 0 &&
                    !profileVm.SkillsToTeach.Any(s => s.Stage >= stageFilter))
                    continue;

                if (!string.IsNullOrEmpty(locationFilter) &&
                    profileVm.Location != locationFilter)
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

            var model = new ExploreViewModel
            {
                MaxCatalogStage = db.SkillStages.Max(s => (int?)s.StageNumber) ?? 7,
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
        public ActionResult Skills(string q = "")
        {
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q[..100];
            var currentUserId = User.Identity.GetUserId();
            var publicTeachers = (from userSkill in db.UserSkills
                                  join info in db.UserInformations on userSkill.UserId equals info.UserId
                                  where userSkill.Status == "Teaching" && info.IsPublic && !info.IsHidden
                                  select new { userSkill.SkillId, info.UserId, info.FullName }).ToList();
            var skills = db.Skills.Include(s => s.SkillCategory).Include(s => s.SkillStages).ToList();
            var catalogEditorEmail = Environment.GetEnvironmentVariable("SKILLBRIDGE_MODERATOR_EMAIL");
            var model = new GuestExploreViewModel
            {
                Query = q,
                CanReviewSuggestions = currentUserId != null && !string.IsNullOrWhiteSpace(catalogEditorEmail) &&
                    db.Users.Any(u => u.Id == currentUserId && u.Email.ToLower() == catalogEditorEmail.Trim().ToLower()),
                MySuggestions = currentUserId == null ? new() : db.SkillSuggestions
                    .Where(s => s.UserId == currentUserId).OrderByDescending(s => s.CreatedAt).Take(10).ToList(),
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
        public ActionResult Detail(int id)
        {
            var skill = db.Skills.Include(s => s.SkillCategory).Include(s => s.SkillStages)
                .FirstOrDefault(s => s.Id == id);
            if (skill == null) return NotFound();
            var userId = User.Identity.GetUserId();
            var teachers = (from userSkill in db.UserSkills
                            join info in db.UserInformations on userSkill.UserId equals info.UserId
                            where userSkill.SkillId == id && userSkill.Status == "Teaching" &&
                                userSkill.KnownUpToStage > 0 && !info.IsHidden &&
                                (info.IsPublic || userId != null) && info.UserId != userId
                            select new { info.UserId, info.FullName }).ToList()
                .Where(t => userId == null || !BlockRules.EitherBlocked(db, userId, t.UserId))
                .Select(t => new GuestTeacherViewModel { UserId = t.UserId, FullName = t.FullName }).ToList();
            return View(new SkillDetailViewModel
            {
                SkillId = skill.Id,
                SkillName = skill.Name,
                Description = skill.Description,
                CategoryName = skill.SkillCategory?.Name,
                Stages = skill.SkillStages.OrderBy(s => s.StageNumber).ToList(),
                Teachers = teachers,
                CommunityId = db.Communities.Where(c => c.SkillId == id).Select(c => (int?)c.Id).FirstOrDefault()
            });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult SuggestSkill(string name, string categoryName, string reason)
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
            if (db.Skills.Any(s => s.Name.ToLower() == name.ToLower()))
            {
                TempData["SkillNotice"] = "That skill is already in the directory.";
                return RedirectToAction(nameof(Skills));
            }
            if (db.SkillSuggestions.Any(s => s.UserId == userId && s.Status == "Pending" &&
                s.Name.ToLower() == name.ToLower()))
            {
                TempData["SkillNotice"] = "You already suggested that skill.";
                return RedirectToAction(nameof(Skills));
            }
            db.SkillSuggestions.Add(new SkillSuggestion
            {
                UserId = userId, Name = name, CategoryName = categoryName, Reason = reason
            });
            db.SaveChanges();
            TempData["SkillNotice"] = "Thanks. Your suggestion is in review.";
            return RedirectToAction(nameof(Skills));
        }
    }
}
