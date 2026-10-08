

using SkillBridge.Helpers;
using SkillBridge.Models;
using SkillBridge.Services;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

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
                IsPublic = userInfo?.IsPublic ?? false,
                IsHidden = userInfo?.IsHidden ?? false,
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
                IsPublic = userInfo.IsPublic,
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

            userInfo.FullName = model.FullName.Trim();
            userInfo.Bio = model.Bio.Trim();
            userInfo.Profession = model.Profession.Trim();
            userInfo.Location = model.Location.Trim();
            userInfo.Age = model.Age;
            userInfo.IsPublic = model.IsPublic;

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
        [AllowAnonymous]
        public ActionResult PublicProfile(string id)
        {
            if (id == null) return NotFound();

            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var userInfo = db.UserInformations.FirstOrDefault(ui => ui.UserId == id);
            if (userInfo == null || (userInfo.IsHidden && id != User.Identity.GetUserId()) ||
                (!User.Identity.IsAuthenticated && !userInfo.IsPublic))
                return NotFound();
            var currentUserId = User.Identity.GetUserId();
            if (User.Identity.IsAuthenticated && id != currentUserId &&
                BlockRules.EitherBlocked(db, currentUserId, id)) return StatusCode(403);

            var userSkills = db.UserSkills
                .Include(us => us.Skill.SkillCategory)
                .Include(us => us.Skill.SkillStages)
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
                    TotalStages = skill.Skill.SkillStages.Count,
                    RequestStatus = visitorWantsThisSkill
                        ? (existingRequest != null
                            ? (existingRequest.Status == "Pending" ? "Pending" :
                               existingRequest.Status == "Accepted" ? "Accepted" : "Declined")
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
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public JsonResult SendSkillRequest(int userSkillId, string profileId, string goal, string pace, string firstMeetingIdea)
        {
            var currentUserId = User.Identity.GetUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "You must be logged in." });
            goal = (goal ?? "").Trim();
            pace = (pace ?? "").Trim();
            firstMeetingIdea = (firstMeetingIdea ?? "").Trim();
            if (goal.Length is < 1 or > 500 || pace.Length is < 1 or > 100 || firstMeetingIdea.Length > 300)
                return Json(new { success = false, message = "Add a goal and pace; keep each field within its limit." });
            if (BlockRules.EitherBlocked(db, currentUserId, profileId))
                return Json(new { success = false, message = "You cannot send a request to this member." });

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
                Goal = goal,
                Pace = pace,
                FirstMeetingIdea = firstMeetingIdea,
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

            return Json(new { success = true, requestId = request.Id });
        }

        public ActionResult Requests()
        {
            var userId = User.Identity.GetUserId();
            var requests = db.SkillRequests.Include(r => r.Skill).Include(r => r.Requester).Include(r => r.Receiver)
                .Where(r => r.RequesterId == userId || r.ReceiverId == userId)
                .OrderByDescending(r => r.CreatedAt).ToList();
            return View(requests);
        }

        public ActionResult RequestDetails(int id)
        {
            var userId = User.Identity.GetUserId();
            var request = db.SkillRequests.Include(r => r.Skill).Include(r => r.Requester).Include(r => r.Receiver)
                .FirstOrDefault(r => r.Id == id && (r.RequesterId == userId || r.ReceiverId == userId));
            if (request == null) return NotFound();
            return View(request);
        }

        public ActionResult BlockedMembers()
        {
            var userId = User.Identity.GetUserId();
            var blocks = db.MemberBlocks.Where(b => b.BlockerId == userId).OrderByDescending(b => b.CreatedAt).ToList();
            var blockedIds = blocks.Select(b => b.BlockedId).ToList();
            var names = db.UserInformations.Where(info => blockedIds.Contains(info.UserId))
                .ToDictionary(info => info.UserId, info => info.FullName);
            ViewBag.Names = names;
            return View(blocks);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Block(string id)
        {
            var userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(id) || id == userId || !db.Users.Any(u => u.Id == id)) return NotFound();
            if (!db.MemberBlocks.Any(b => b.BlockerId == userId && b.BlockedId == id))
            {
                db.MemberBlocks.Add(new MemberBlock { BlockerId = userId, BlockedId = id });
                foreach (var request in db.SkillRequests.Where(r => r.Status == "Pending" &&
                    ((r.RequesterId == userId && r.ReceiverId == id) ||
                     (r.RequesterId == id && r.ReceiverId == userId)))) request.Status = "Declined";
                foreach (var interaction in db.Interactions.Where(i => i.Status == "Ongoing" &&
                    ((i.User1Id == userId && i.User2Id == id) ||
                     (i.User1Id == id && i.User2Id == userId))))
                {
                    interaction.Status = "Ended";
                    interaction.EndReason = "A member blocked further contact.";
                    interaction.EndedByUserId = userId;
                    interaction.EndedAt = DateTime.Now;
                }
                foreach (var notification in db.Notifications.Where(n => n.Type == "SkillRequest" &&
                    (n.UserId == userId || n.UserId == id) && db.SkillRequests.Any(r => r.Id == n.ReferenceId &&
                        ((r.RequesterId == userId && r.ReceiverId == id) ||
                         (r.RequesterId == id && r.ReceiverId == userId)))))
                {
                    notification.Type = "Info";
                    notification.Message = "This exchange request is no longer available.";
                    notification.IsRead = true;
                }
                db.SaveChanges();
            }
            return RedirectToAction(nameof(BlockedMembers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Unblock(string id)
        {
            var userId = User.Identity.GetUserId();
            var block = db.MemberBlocks.FirstOrDefault(b => b.BlockerId == userId && b.BlockedId == id);
            if (block == null) return NotFound();
            db.MemberBlocks.Remove(block);
            db.SaveChanges();
            return RedirectToAction(nameof(BlockedMembers));
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult ReportProfile(string id, string reason)
        {
            var userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(id) || id == userId || !db.Users.Any(u => u.Id == id)) return NotFound();
            reason = (reason ?? "").Trim();
            if (reason.Length is < 1 or > 500) return BadRequest("Give a reason of at most 500 characters.");
            var report = db.ProfileReports.FirstOrDefault(r => r.ReporterId == userId && r.ReportedUserId == id);
            if (report == null) db.ProfileReports.Add(new ProfileReport
                { ReporterId = userId, ReportedUserId = id, Reason = reason });
            else { report.Reason = reason; report.Status = "Pending"; report.CreatedAt = DateTime.Now; }
            db.SaveChanges();
            TempData["ProfileNotice"] = "Your report has been sent for review.";
            return RedirectToAction(nameof(PublicProfile), new { id });
        }



        //////////////////////////////////////////////////////////////////
    }
}
