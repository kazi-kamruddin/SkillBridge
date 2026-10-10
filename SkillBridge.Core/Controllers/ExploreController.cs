
using SkillBridge.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SkillBridge.Helpers;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class ExploreController : Controller
    {
        private readonly ApplicationDbContext db;

        public ExploreController(ApplicationDbContext db) => this.db = db;

        private readonly List<string> BangladeshDivisions = new List<string>
        {
            "Dhaka", "Chattogram", "Khulna", "Barishal", "Sylhet", "Mymensingh", "Rajshahi", "Rangpur"
        };




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

                bool isBestMatch = userLearningSkills.Any(l => teachingSkillsIds.Contains(l.SkillId)) &&
                                   userTeachingSkills.Any(t => learningSkills.Select(ls => ls.SkillId).Contains(t.SkillId));

                bool isPartialMatch = !isBestMatch && userTeachingSkills.Any(t => learningSkills.Select(ls => ls.SkillId).Contains(t.SkillId));

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
                    AverageRating = averageRating,
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
            var publicTeachers = (from userSkill in db.UserSkills
                                  join info in db.UserInformations on userSkill.UserId equals info.UserId
                                  where userSkill.Status == "Teaching" && info.IsPublic && !info.IsHidden
                                  select new { userSkill.SkillId, info.UserId, info.FullName }).ToList();
            var skills = db.Skills.Include(s => s.SkillCategory).Include(s => s.SkillStages).ToList();
            var model = new GuestExploreViewModel
            {
                Query = q,
                Skills = SkillSearch.Rank(skills, q).Select(skill => new GuestSkillViewModel
                {
                    SkillName = skill.Name,
                    CategoryName = skill.SkillCategory.Name,
                    Teachers = publicTeachers.Where(t => t.SkillId == skill.Id)
                        .Select(t => new GuestTeacherViewModel { UserId = t.UserId, FullName = t.FullName }).ToList()
                }).ToList()
            };
            return View("GuestIndex", model);
        }
    }
}
