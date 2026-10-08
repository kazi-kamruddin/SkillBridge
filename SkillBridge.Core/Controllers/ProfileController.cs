

using SkillBridge.Helpers;
using SkillBridge.Models;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private readonly ApplicationDbContext db;

        public ProfileController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, ApplicationDbContext db)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            this.db = db;
        }


        ////////////////////////////////////////////////////////////////////////////
        // GET: /Profile/Index
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var hasPassword = await UserManager.HasPasswordAsync(user);
            var userInfo = db.UserInformations.FirstOrDefault(u => u.UserId == userId);

            var userSkills = db.UserSkills
                .Where(us => us.UserId == userId)
                .Select(us => new UserSkillViewModel
                {
                    SkillName = us.Skill.Name,
                    CategoryName = us.Skill.SkillCategory.Name,
                    KnownUpToStage = us.KnownUpToStage ?? 0,
                    TotalStages = us.Skill.SkillStages.Count(),
                    Status = us.Status
                }).ToList();

            var teachingSkills = userSkills.Where(s => s.Status == "Teaching").ToList();
            var learningSkills = userSkills.Where(s => s.Status == "Learning").ToList();

            var userRatings = db.UserRatings.FirstOrDefault(ur => ur.UserId == userId);

            double averageRating = (userRatings != null && userRatings.RatingsReceived > 0)
                ? (double)userRatings.AccumulatedRating / userRatings.RatingsReceived
                : 0;

            int ratingsReceived = userRatings?.RatingsReceived ?? 0;
            int interactionsCompleted = userRatings?.InteractionsCompleted ?? 0;

            var model = new IndexViewModel
            {
                HasPassword = hasPassword,
                FullName = userInfo?.FullName ?? "",
                Email = user.Email,
                Bio = userInfo?.Bio ?? "",
                Profession = userInfo?.Profession ?? "",
                Location = userInfo?.Location ?? "",
                Age = userInfo?.Age ?? 0,
                TeachingSkills = teachingSkills,
                LearningSkills = learningSkills,
                AverageRating = averageRating,
                RatingsReceived = ratingsReceived,
                InteractionsCompleted = interactionsCompleted,
                ProfileImageUrl = ProfileImageHelper.GetRandomProfileImage()
            };

            return View(model);
        }





        ////////////////////////////////////////////////////////////////////////////
        // GET: /Profile/UpdateProfile
        public async Task<ActionResult> UpdateProfile()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var userInfo = db.UserInformations.FirstOrDefault(u => u.UserId == userId);
            if (userInfo == null)
                return RedirectToAction("Index", "CompleteProfile");
            var userSkills = db.UserSkills.Where(us => us.UserId == userId).ToList();

            var skillCategories = db.SkillCategories
                .Include("Skills.SkillStages")
                .ToList();

            var model = new UpdateProfileViewModel
            {
                FullName = userInfo.FullName,
                Email = user.Email,
                Bio = userInfo.Bio,
                Profession = userInfo.Profession,
                Location = userInfo.Location,
                Age = userInfo.Age,
                SkillsToLearn = userSkills.Where(s => s.Status == "Learning").Select(s => s.SkillId).ToList(),
                SkillsIKnow = userSkills.Where(s => s.Status == "Teaching").Select(s => new UpdateProfileViewModel.UserKnownSkill
                {
                    SkillId = s.SkillId,
                    KnownUpToStage = s.KnownUpToStage ?? 1
                }).ToList(),
                AllSkillCategories = skillCategories
            };

            return View(model);
        }



        ////////////////////////////////////////////////////////////////////////////
        // POST: /Profile/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UpdateProfileViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var userInfo = db.UserInformations.FirstOrDefault(u => u.UserId == userId);
            if (userInfo == null)
                return RedirectToAction("Index", "CompleteProfile");

            var learningIds = model.SkillsToLearn ?? new List<int>();
            var teachingSkills = model.SkillsIKnow ?? new List<UpdateProfileViewModel.UserKnownSkill>();
            var validSkillIds = new HashSet<int>(db.Skills.Select(s => s.Id).ToList());
            var maxStageBySkill = db.SkillStages.ToList()
                .GroupBy(s => s.SkillId)
                .ToDictionary(g => g.Key, g => g.Max(s => s.StageNumber));
            if (!learningIds.Any() || !teachingSkills.Any())
                ModelState.AddModelError("", "Choose at least one skill to learn and one skill to teach.");
            if (learningIds.Any(id => !validSkillIds.Contains(id)) ||
                teachingSkills.Any(s => !validSkillIds.Contains(s.SkillId) ||
                    !maxStageBySkill.ContainsKey(s.SkillId) ||
                    s.KnownUpToStage < 1 || s.KnownUpToStage > maxStageBySkill[s.SkillId]))
                ModelState.AddModelError("", "Choose valid skills and stages.");
            if (teachingSkills.Select(s => s.SkillId).Distinct().Count() != teachingSkills.Count ||
                teachingSkills.Any(s => learningIds.Contains(s.SkillId)))
                ModelState.AddModelError("", "A skill cannot appear twice or be both taught and learned.");

            if (!ModelState.IsValid)
            {
                model.AllSkillCategories = db.SkillCategories.Include("Skills.SkillStages").ToList();
                return View(model);
            }

            userInfo.FullName = model.FullName;
            userInfo.Bio = model.Bio;
            userInfo.Profession = model.Profession;
            userInfo.Location = model.Location;
            userInfo.Age = model.Age;

            var existingSkills = db.UserSkills.Where(us => us.UserId == userId).ToList();
            using (var transaction = db.Database.BeginTransaction())
            {
                db.UserSkills.RemoveRange(existingSkills);
                db.SaveChanges();

                foreach (var skillId in learningIds.Distinct())
                {
                    db.UserSkills.Add(new UserSkill
                    {
                        UserId = userId,
                        SkillId = skillId,
                        Status = "Learning",
                        KnownUpToStage = 0
                    });
                }
                foreach (var sk in teachingSkills)
                {
                    db.UserSkills.Add(new UserSkill
                    {
                        UserId = userId,
                        SkillId = sk.SkillId,
                        Status = "Teaching",
                        KnownUpToStage = sk.KnownUpToStage
                    });
                }

                db.SaveChanges();
                transaction.Commit();
            }
            return RedirectToAction("Index");
        }



        ////////////////////////////////////////////////////////////////////////////
        // GET: /Profile/ChangePassword
        public ActionResult ChangePassword() => View();



        ////////////////////////////////////////////////////////////////////////////
        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                if (user != null)
                    await SignInManager.RefreshSignInAsync(user);

                ViewBag.StatusMessage = "Your password has been changed successfully.";
                return View();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        // GET: /Profile/PublicProfile
        public ActionResult PublicProfile(string id)
        {
            if (id == null) return NotFound();

            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var userInfo = db.UserInformations.FirstOrDefault(ui => ui.UserId == id);
            var currentUserId = User.Identity.GetUserId();

            var userSkills = db.UserSkills
                .Include(us => us.Skill.SkillCategory)
                .Where(us => us.UserId == id)
                .ToList();

            var visitorLearningSkillIds = db.UserSkills
                .Where(us => us.UserId == currentUserId && us.Status == "Learning")
                .Select(us => us.SkillId)
                .ToList();

            var skillsToTeachVm = new List<SkillViewModel>();
            foreach (var skill in userSkills.Where(us => us.Status == "Teaching"))
            {
                bool visitorWantsThisSkill = visitorLearningSkillIds.Contains(skill.SkillId);

                var existingRequest = db.SkillRequests
                    .Where(r => r.SkillId == skill.SkillId &&
                                r.RequesterId == currentUserId &&
                                r.ReceiverId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();

                skillsToTeachVm.Add(new SkillViewModel
                {
                    UserSkillId = skill.Id,
                    SkillId = skill.SkillId,
                    SkillName = skill.Skill.Name,
                    Stage = skill.KnownUpToStage ?? 1,
                    RequestStatus = visitorWantsThisSkill
                        ? (existingRequest != null
                            ? (existingRequest.Status == "Pending" ? "Pending" : "Declined")
                            : "None")
                        : "Hidden"
                });
            }

            var userRatings = db.UserRatings.FirstOrDefault(ur => ur.UserId == id);

            double averageRating = (userRatings != null && userRatings.RatingsReceived > 0)
                ? (double)userRatings.AccumulatedRating / userRatings.RatingsReceived
                : 0;

            int ratingsReceived = userRatings?.RatingsReceived ?? 0;
            int interactionsCompleted = userRatings?.InteractionsCompleted ?? 0;

            var model = new PublicProfileViewModel
            {
                UserId = user.Id,
                FullName = userInfo?.FullName ?? "",
                Profession = userInfo?.Profession ?? "",
                Location = userInfo?.Location ?? "",
                Bio = userInfo?.Bio ?? "",
                SkillsToTeach = skillsToTeachVm,
                SkillsToLearn = userSkills
                    .Where(us => us.Status == "Learning")
                    .Select(us => new SkillViewModel
                    {
                        SkillId = us.SkillId,
                        SkillName = us.Skill.Name,
                        Stage = us.KnownUpToStage ?? 0,
                        RequestStatus = "None"
                    }).ToList(),
                AverageRating = averageRating,
                RatingsReceived = ratingsReceived,
                InteractionsCompleted = interactionsCompleted,
                ProfileImageUrl = ProfileImageHelper.GetRandomProfileImage()
            };

            return View(model);
        }



        /////////////////////////////////////////////////////////////////////////////
        // POST: /Profile/SendSkillRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendSkillRequest(int userSkillId, string profileId)
        {
            var currentUserId = User.Identity.GetUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "You must be logged in." });

            var userSkill = db.UserSkills
                .Include("Skill")
                .FirstOrDefault(us => us.Id == userSkillId && us.UserId == profileId);

            if (userSkill == null || userSkill.Status != "Teaching")
                return Json(new { success = false, message = "Skill not found for this user." });

            if (userSkill.UserId == currentUserId)
                return Json(new { success = false, message = "You cannot request your own skill." });

            if (!db.UserSkills.Any(us => us.UserId == currentUserId &&
                us.SkillId == userSkill.SkillId && us.Status == "Learning"))
                return Json(new { success = false, message = "Add this skill to your learning list first." });

            var existingRequest = db.SkillRequests
                .FirstOrDefault(r => r.SkillId == userSkill.SkillId &&
                                     r.RequesterId == currentUserId &&
                                     r.ReceiverId == userSkill.UserId &&
                                     r.Status == "Pending");

            if (existingRequest != null)
                return Json(new { success = false, message = "Request already sent." });

            var request = new SkillRequest
            {
                SkillId = userSkill.SkillId,
                RequesterId = currentUserId,
                ReceiverId = userSkill.UserId,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            using (var transaction = db.Database.BeginTransaction())
            {
                db.SkillRequests.Add(request);
                db.SaveChanges();

                var requester = db.Users.Find(currentUserId);
                var notification = new Notification
                {
                    UserId = userSkill.UserId,
                    Type = "SkillRequest",
                    ReferenceId = request.Id,
                    Message = $"{requester.UserName} requested your skill: {userSkill.Skill.Name}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                db.Notifications.Add(notification);
                db.SaveChanges();
                transaction.Commit();
            }

            return Json(new { success = true });
        }



        //////////////////////////////////////////////////////////////////
    }
}
