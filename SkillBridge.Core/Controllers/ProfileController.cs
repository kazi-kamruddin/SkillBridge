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
        private readonly CloudinaryImageService images;
        private readonly ILogger<ProfileController> logger;

        public ProfileController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, ApplicationDbContext db, CloudinaryImageService images,
            ILogger<ProfileController> logger)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            this.db = db;
            this.images = images;
            this.logger = logger;
        }

        ////////////////////////////////////////////////////////////////////////////
        // GET: /Profile/Index
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var hasPassword = await UserManager.HasPasswordAsync(user);
            var googleLinked = (await UserManager.GetLoginsAsync(user)).Any(login => login.LoginProvider == "Google");
            var userInfo = await db.UserInformations.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (userInfo == null)
                return RedirectToAction("Index", "CompleteProfile");

            var userSkills = await db.UserSkills
                .AsNoTracking()
                .Where(us => us.UserId == userId)
                .Select(us => new UserSkillViewModel
                {
                    SkillName = us.Skill.Name,
                    CategoryName = us.Skill.SkillCategory.Name,
                    KnownUpToStage = us.KnownUpToStage ?? 0,
                    TotalStages = us.Skill.SkillStages.Count(),
                    Status = us.Status
                }).ToListAsync();

            var teachingSkills = userSkills.Where(s => s.Status == "Teaching").ToList();
            var learningSkills = userSkills.Where(s => s.Status == "Learning").ToList();

            var userRatings = await db.UserRatings.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);

            double averageRating = (userRatings != null && userRatings.RatingsReceived > 0)
                ? (double)userRatings.AccumulatedRating / userRatings.RatingsReceived
                : 0;

            int ratingsReceived = userRatings?.RatingsReceived ?? 0;
            int interactionsCompleted = userRatings?.InteractionsCompleted ?? 0;

            var model = new IndexViewModel
            {
                HasPassword = hasPassword,
                EmailConfirmed = user.EmailConfirmed,
                GoogleEnabled = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKILLBRIDGE_GOOGLE_CLIENT_ID")) &&
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKILLBRIDGE_GOOGLE_CLIENT_SECRET")),
                GoogleLinked = googleLinked,
                AvailabilityNotes = userInfo?.AvailabilityNotes,
                AvailableDaysMask = userInfo?.AvailableDaysMask ?? 0,
                TimeZoneId = userInfo?.TimeZoneId,
                MeetingFormat = userInfo?.MeetingFormat ?? "Either",
                FullName = userInfo?.FullName ?? "",
                Email = user.Email,
                Bio = userInfo?.Bio ?? "",
                Profession = userInfo?.Profession ?? "",
                Location = userInfo?.Location ?? "",
                Age = userInfo?.Age,
                IsPublic = userInfo?.IsPublic ?? false,
                IsHidden = userInfo?.IsHidden ?? false,
                TeachingSkills = teachingSkills,
                LearningSkills = learningSkills,
                AverageRating = averageRating,
                RatingsReceived = ratingsReceived,
                InteractionsCompleted = interactionsCompleted,
                ProfileImageUrl = ProfileImageHelper.GetProfileImage(userInfo?.ProfileImageUrl, userInfo?.FullName)
            };

            return View(model);
        }

        ////////////////////////////////////////////////////////////////////////////
        // GET: /Profile/UpdateProfile
        public async Task<ActionResult> UpdateProfile()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var userInfo = await db.UserInformations.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (userInfo == null)
                return RedirectToAction("Index", "CompleteProfile");
            var userSkills = await db.UserSkills.AsNoTracking().Where(us => us.UserId == userId).ToListAsync();

            var skillCategories = await db.SkillCategories
                .AsNoTracking()
                .Include("Skills.SkillStages")
                .ToListAsync();

            var model = new UpdateProfileViewModel
            {
                FullName = userInfo.FullName,
                Email = user.Email,
                Bio = userInfo.Bio,
                Profession = userInfo.Profession,
                Location = userInfo.Location,
                Age = userInfo.Age,
                IsPublic = userInfo.IsPublic,
                AvailabilityNotes = userInfo.AvailabilityNotes,
                AvailableDays = Enumerable.Range(0, 7).Where(day => (userInfo.AvailableDaysMask & (1 << day)) != 0).ToList(),
                TimeZoneId = userInfo.TimeZoneId,
                MeetingFormat = userInfo.MeetingFormat ?? "Either",
                ExistingProfileImageUrl = ProfileImageHelper.GetProfileImage(userInfo.ProfileImageUrl, userInfo.FullName),
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
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<ActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var userInfo = await db.UserInformations.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userInfo == null)
                return RedirectToAction("Index", "CompleteProfile");

            var learningIds = model.SkillsToLearn ?? new List<int>();
            var teachingSkills = model.SkillsIKnow ?? new List<UpdateProfileViewModel.UserKnownSkill>();
            var validSkillIds = (await db.Skills.AsNoTracking().Select(s => s.Id).ToListAsync()).ToHashSet();
            var allStages = await db.SkillStages.AsNoTracking().ToListAsync();
            var maxStageBySkill = allStages
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
            if (model.MeetingFormat != "Online" && model.MeetingFormat != "In person" && model.MeetingFormat != "Either")
                ModelState.AddModelError(nameof(model.MeetingFormat), "Choose a meeting format.");
            if (model.AvailableDays?.Any(day => day < 0 || day > 6) == true)
                ModelState.AddModelError(nameof(model.AvailableDays), "Choose valid days of the week.");
            if (!string.IsNullOrWhiteSpace(model.TimeZoneId))
            {
                try { TimeZoneInfo.FindSystemTimeZoneById(model.TimeZoneId.Trim()); }
                catch (TimeZoneNotFoundException) { ModelState.AddModelError(nameof(model.TimeZoneId), "Use a valid time zone such as Asia/Dhaka."); }
                catch (InvalidTimeZoneException) { ModelState.AddModelError(nameof(model.TimeZoneId), "Use a valid time zone."); }
            }

            if (!ModelState.IsValid)
            {
                model.AllSkillCategories = await db.SkillCategories.AsNoTracking().Include("Skills.SkillStages").ToListAsync();
                model.ExistingProfileImageUrl = ProfileImageHelper.GetProfileImage(userInfo.ProfileImageUrl, userInfo.FullName);
                return View(model);
            }

            UploadedImage uploaded = null;
            if (model.ProfileImage?.Length > 0)
            {
                try { uploaded = await images.UploadAsync(model.ProfileImage, "skillbridge/avatars", false); }
                catch { ModelState.AddModelError(nameof(model.ProfileImage), "The image could not be uploaded. Please try again.");
                    model.AllSkillCategories = await db.SkillCategories.AsNoTracking().Include("Skills.SkillStages").ToListAsync();
                    model.ExistingProfileImageUrl = ProfileImageHelper.GetProfileImage(userInfo.ProfileImageUrl, userInfo.FullName);
                    return View(model); }
            }
            var previousImagePublicId = userInfo.ProfileImagePublicId;
            userInfo.FullName = model.FullName.Trim();
            userInfo.Bio = model.Bio?.Trim();
            userInfo.Profession = model.Profession?.Trim();
            userInfo.Location = model.Location?.Trim();
            userInfo.Age = model.Age;
            if (uploaded != null)
            {
                userInfo.ProfileImageUrl = uploaded.Url;
                userInfo.ProfileImagePublicId = uploaded.PublicId;
            }
            else if (model.RemoveProfileImage)
            {
                userInfo.ProfileImageUrl = null;
                userInfo.ProfileImagePublicId = null;
            }
            userInfo.IsPublic = model.IsPublic;
            userInfo.AvailabilityNotes = model.AvailabilityNotes?.Trim();
            userInfo.AvailableDaysMask = (model.AvailableDays ?? new()).Distinct().Aggregate(0, (mask, day) => mask | (1 << day));
            userInfo.TimeZoneId = string.IsNullOrWhiteSpace(model.TimeZoneId) ? null : model.TimeZoneId.Trim();
            userInfo.MeetingFormat = model.MeetingFormat;

            var existingSkills = await db.UserSkills.Where(us => us.UserId == userId).ToListAsync();
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                db.UserSkills.RemoveRange(existingSkills);
                await db.SaveChangesAsync();

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

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            if (!string.IsNullOrWhiteSpace(previousImagePublicId) &&
                previousImagePublicId != userInfo.ProfileImagePublicId)
            {
                try { await images.DeletePublicAsync(previousImagePublicId); }
                catch { logger.LogWarning("Could not remove an old profile image from Cloudinary."); }
            }
            return RedirectToAction("Index");
        }

        ////////////////////////////////////////////////////////////////////////////
        // GET: /Profile/ChangePassword
        public ActionResult ChangePassword() => View();

        public async Task<ActionResult> SetPassword()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null || await UserManager.HasPasswordAsync(user)) return RedirectToAction(nameof(Index));
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null || await UserManager.HasPasswordAsync(user)) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);
            var result = await UserManager.AddPasswordAsync(user, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }
            await SignInManager.RefreshSignInAsync(user);
            TempData["AccountNotice"] = "Password added. You can now use email and password to sign in.";
            return RedirectToAction(nameof(Index));
        }

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
        // GET: /Profile/PublicProfile
        [AllowAnonymous]
        public async Task<ActionResult> PublicProfile(string id)
        {
            if (id == null) return NotFound();

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var userInfo = await db.UserInformations.AsNoTracking().FirstOrDefaultAsync(ui => ui.UserId == id);
            if (userInfo == null || (userInfo.IsHidden && id != User.Identity.GetUserId()) ||
                (!User.Identity.IsAuthenticated && !userInfo.IsPublic))
                return NotFound();
            var currentUserId = User.Identity.GetUserId();
            if (User.Identity.IsAuthenticated && id != currentUserId &&
                await BlockRules.EitherBlockedAsync(db, currentUserId, id)) return StatusCode(403);

            var userSkills = await db.UserSkills
                .AsNoTracking()
                .Include(us => us.Skill.SkillCategory)
                .Include(us => us.Skill.SkillStages)
                .Where(us => us.UserId == id)
                .ToListAsync();

            var visitorLearningSkillIds = currentUserId != null
                ? await db.UserSkills
                    .AsNoTracking()
                    .Where(us => us.UserId == currentUserId && us.Status == "Learning")
                    .Select(us => us.SkillId)
                    .ToListAsync()
                : new List<int>();

            var skillsToTeachVm = new List<SkillViewModel>();
            foreach (var skill in userSkills.Where(us => us.Status == "Teaching"))
            {
                bool visitorWantsThisSkill = visitorLearningSkillIds.Contains(skill.SkillId);

                var existingRequest = currentUserId != null
                    ? await db.SkillRequests
                        .AsNoTracking()
                        .Where(r => r.SkillId == skill.SkillId &&
                                    r.RequesterId == currentUserId &&
                                    r.ReceiverId == id)
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefaultAsync()
                    : null;

                skillsToTeachVm.Add(new SkillViewModel
                {
                    UserSkillId = skill.Id,
                    SkillId = skill.SkillId,
                    SkillName = skill.Skill.Name,
                    Stage = skill.KnownUpToStage ?? 0,
                    TotalStages = skill.Skill.SkillStages.Count,
                    RequestStatus = skill.KnownUpToStage.GetValueOrDefault() < 1 ? "Hidden" : visitorWantsThisSkill
                        ? (existingRequest != null
                            ? (existingRequest.Status == "Pending" ? "Pending" :
                               existingRequest.Status == "Accepted" ? "Accepted" :
                               existingRequest.Status == "Withdrawn" ? "Withdrawn" : "Declined")
                            : "None")
                        : "Hidden"
                });
            }

            var userRatings = await db.UserRatings.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == id);

            double averageRating = (userRatings != null && userRatings.RatingsReceived > 0)
                ? (double)userRatings.AccumulatedRating / userRatings.RatingsReceived
                : 0;

            int ratingsReceived = userRatings?.RatingsReceived ?? 0;
            int interactionsCompleted = userRatings?.InteractionsCompleted ?? 0;

            var isSaved = currentUserId != null && await db.SavedProfiles.AsNoTracking().AnyAsync(s => s.UserId == currentUserId && s.TargetUserId == id);
            var canKnock = currentUserId != null &&
                await db.UserSkills.AsNoTracking().AnyAsync(s => s.UserId == currentUserId && s.Status == "Teaching" && s.KnownUpToStage > 0) &&
                visitorLearningSkillIds.Any() &&
                userSkills.Any(s => s.Status == "Teaching" && s.KnownUpToStage > 0) &&
                userSkills.Any(s => s.Status == "Learning");

            var model = new PublicProfileViewModel
            {
                UserId = user.Id,
                FullName = userInfo?.FullName ?? "",
                Profession = userInfo?.Profession ?? "",
                Location = userInfo?.Location ?? "",
                Bio = userInfo?.Bio ?? "",
                AvailabilityNotes = userInfo.AvailabilityNotes,
                AvailableDaysMask = userInfo.AvailableDaysMask,
                TimeZoneId = userInfo.TimeZoneId,
                MeetingFormat = userInfo.MeetingFormat ?? "Either",
                IsSaved = isSaved,
                CanKnock = canKnock,
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
                ProfileImageUrl = ProfileImageHelper.GetProfileImage(userInfo?.ProfileImageUrl, userInfo?.FullName)
            };

            return View(model);
        }

        /////////////////////////////////////////////////////////////////////////////
        // POST: /Profile/SendSkillRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<JsonResult> SendSkillRequest(int userSkillId, string profileId, string goal, string pace, string firstMeetingIdea)
        {
            var currentUserId = User.Identity.GetUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "You must be logged in." });
            if (!await db.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == currentUserId && us.Status == "Teaching" && us.KnownUpToStage > 0) ||
                !await db.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == currentUserId && us.Status == "Learning"))
                return Json(new { success = false, message = "Add a skill to teach and a skill to learn before requesting an exchange." });
            goal = (goal ?? "").Trim();
            pace = (pace ?? "").Trim();
            firstMeetingIdea = (firstMeetingIdea ?? "").Trim();
            if (goal.Length is < 1 or > 500 || pace.Length is < 1 or > 100 || firstMeetingIdea.Length > 300)
                return Json(new { success = false, message = "Add a goal and pace; keep each field within its limit." });
            if (await BlockRules.EitherBlockedAsync(db, currentUserId, profileId))
                return Json(new { success = false, message = "You cannot send a request to this member." });
            if (!await db.UserInformations.AsNoTracking().AnyAsync(info => info.UserId == profileId && !info.IsHidden))
                return Json(new { success = false, message = "This member is not available for exchange requests." });

            var userSkill = await db.UserSkills
                .Include("Skill")
                .FirstOrDefaultAsync(us => us.Id == userSkillId && us.UserId == profileId);

            if (userSkill == null || userSkill.Status != "Teaching")
                return Json(new { success = false, message = "Skill not found for this user." });
            if (userSkill.KnownUpToStage.GetValueOrDefault() < 1)
                return Json(new { success = false, message = "This member has not listed a teachable level for this skill." });

            if (userSkill.UserId == currentUserId)
                return Json(new { success = false, message = "You cannot request your own skill." });

            if (!await db.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == currentUserId &&
                us.SkillId == userSkill.SkillId && us.Status == "Learning"))
                return Json(new { success = false, message = "Add this skill to your learning list first." });

            var existingRequest = await db.SkillRequests
                .FirstOrDefaultAsync(r => r.SkillId == userSkill.SkillId &&
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

            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                db.SkillRequests.Add(request);
                await db.SaveChangesAsync();

                var currentUserName = await MemberNames.GetAsync(db, currentUserId);
                var notification = new Notification
                {
                    UserId = userSkill.UserId,
                    Type = "SkillRequest",
                    ReferenceId = request.Id,
                    Message = $"{currentUserName} requested your skill: {userSkill.Skill.Name}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return Json(new { success = true, requestId = request.Id });
        }

        public async Task<ActionResult> Requests()
        {
            var userId = User.Identity.GetUserId();
            var requests = await db.SkillRequests
                .AsNoTracking()
                .Include(r => r.Skill).Include(r => r.Requester).Include(r => r.Receiver)
                .Where(r => r.RequesterId == userId || r.ReceiverId == userId)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.MemberNames = await MemberNames.ForAsync(db, requests.SelectMany(r => new[] { r.RequesterId, r.ReceiverId }));
            return View(requests);
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> WithdrawSkillRequest(int id)
        {
            var userId = User.Identity.GetUserId();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var request = await db.SkillRequests.Include(r => r.Skill)
                .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return StatusCode(409);
            var claimed = await db.SkillRequests.Where(r => r.Id == id && r.RequesterId == userId && r.Status == "Pending")
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "Withdrawn"));
            if (claimed != 1) return StatusCode(409);
            request.Status = "Withdrawn";
            var notifications = await db.Notifications.Where(n => n.UserId == request.ReceiverId &&
                n.Type == "SkillRequest" && n.ReferenceId == id).ToListAsync();
            foreach (var notification in notifications)
            {
                notification.Type = "RequestUpdate";
                notification.Message = $"The request for {request.Skill.Name} was withdrawn.";
                notification.IsRead = false;
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(RequestDetails), new { id });
        }

        public async Task<ActionResult> RequestDetails(int id)
        {
            var userId = User.Identity.GetUserId();
            var request = await db.SkillRequests
                .AsNoTracking()
                .Include(r => r.Skill).Include(r => r.Requester).Include(r => r.Receiver)
                .FirstOrDefaultAsync(r => r.Id == id && (r.RequesterId == userId || r.ReceiverId == userId));
            if (request == null) return NotFound();
            var names = await MemberNames.ForAsync(db, new[] { request.RequesterId, request.ReceiverId });
            ViewBag.RequesterName = MemberNames.Get(names, request.RequesterId);
            ViewBag.ReceiverName = MemberNames.Get(names, request.ReceiverId);
            if (request.ReceiverId == userId && request.Status == "Pending")
            {
                ViewBag.RequestNotificationId = await db.Notifications
                    .AsNoTracking()
                    .Where(n => n.UserId == userId && n.Type == "SkillRequest" && n.ReferenceId == id)
                    .Select(n => n.Id).FirstOrDefaultAsync();
                var wanted = await db.UserSkills
                    .AsNoTracking()
                    .Where(us => us.UserId == userId && us.Status == "Learning")
                    .Select(us => us.SkillId).ToListAsync();
                ViewBag.ReciprocalSkills = await db.UserSkills
                    .AsNoTracking()
                    .Include(us => us.Skill)
                    .Where(us => us.UserId == request.RequesterId && us.Status == "Teaching" &&
                        us.KnownUpToStage > 0 && us.SkillId != request.SkillId && wanted.Contains(us.SkillId))
                    .Select(us => new SkillViewModel { SkillId = us.SkillId, SkillName = us.Skill.Name })
                    .ToListAsync();
            }
            return View(request);
        }

        public async Task<ActionResult> BlockedMembers()
        {
            var userId = User.Identity.GetUserId();
            var blocks = await db.MemberBlocks
                .AsNoTracking()
                .Where(b => b.BlockerId == userId).OrderByDescending(b => b.CreatedAt).ToListAsync();
            var blockedIds = blocks.Select(b => b.BlockedId).ToList();
            var names = await db.UserInformations
                .AsNoTracking()
                .Where(info => blockedIds.Contains(info.UserId))
                .ToDictionaryAsync(info => info.UserId, info => info.FullName);
            ViewBag.Names = names;
            return View(blocks);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Block(string id)
        {
            var userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(id) || id == userId || !await db.Users.AsNoTracking().AnyAsync(u => u.Id == id)) return NotFound();
            if (!await db.MemberBlocks.AnyAsync(b => b.BlockerId == userId && b.BlockedId == id))
            {
                db.MemberBlocks.Add(new MemberBlock { BlockerId = userId, BlockedId = id });
                var pendingRequests = await db.SkillRequests.Where(r => r.Status == "Pending" &&
                    ((r.RequesterId == userId && r.ReceiverId == id) ||
                     (r.RequesterId == id && r.ReceiverId == userId))).ToListAsync();
                foreach (var request in pendingRequests) request.Status = "Declined";

                var ongoingInteractions = await db.Interactions.Where(i => i.Status == "Ongoing" &&
                    ((i.User1Id == userId && i.User2Id == id) ||
                     (i.User1Id == id && i.User2Id == userId))).ToListAsync();
                foreach (var interaction in ongoingInteractions)
                {
                    interaction.Status = "Ended";
                    interaction.EndReason = "A member blocked further contact.";
                    interaction.EndedByUserId = userId;
                    interaction.EndedAt = DateTime.Now;
                }

                var requestIds = pendingRequests.Select(r => (int?)r.Id).ToList();
                var notifications = await db.Notifications.Where(n => n.Type == "SkillRequest" &&
                    (n.UserId == userId || n.UserId == id) && requestIds.Contains(n.ReferenceId)).ToListAsync();
                foreach (var notification in notifications)
                {
                    notification.Type = "Info";
                    notification.Message = "This exchange request is no longer available.";
                    notification.IsRead = true;
                }
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(BlockedMembers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Unblock(string id)
        {
            var userId = User.Identity.GetUserId();
            var block = await db.MemberBlocks.FirstOrDefaultAsync(b => b.BlockerId == userId && b.BlockedId == id);
            if (block == null) return NotFound();
            db.MemberBlocks.Remove(block);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(BlockedMembers));
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> ReportProfile(string id, string reason)
        {
            var userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(id) || id == userId || !await db.Users.AsNoTracking().AnyAsync(u => u.Id == id)) return NotFound();
            reason = (reason ?? "").Trim();
            if (reason.Length is < 1 or > 500) return BadRequest("Give a reason of at most 500 characters.");
            var report = await db.ProfileReports.FirstOrDefaultAsync(r => r.ReporterId == userId && r.ReportedUserId == id);
            if (report == null) db.ProfileReports.Add(new ProfileReport
                { ReporterId = userId, ReportedUserId = id, Reason = reason });
            else { report.Reason = reason; report.Status = "Pending"; report.CreatedAt = DateTime.Now; }
            await db.SaveChangesAsync();
            TempData["ProfileNotice"] = "Your report has been sent for review.";
            return RedirectToAction(nameof(PublicProfile), new { id });
        }

        //////////////////////////////////////////////////////////////////
    }
}
