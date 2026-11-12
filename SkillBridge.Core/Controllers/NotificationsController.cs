using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SkillBridge.Models;
using SkillBridge.Helpers;
using SkillBridge.Hubs;
using SkillBridge.Services;
using Microsoft.EntityFrameworkCore;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext = null,
            ILogger<NotificationsController> logger = null)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        ////////////////////////////////////////////////////////////////////////////
        // GET: /Notifications
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Type == "SkillRequest")
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications.Select(n =>
            {
                n.Message = MemberNames.NotificationText(n.Message);
                return n;
            }).ToList());
        }

        ////////////////////////////////////////////////////////////////////////////
        // AJAX: Get notifications as JSON for badge
        [HttpGet]
        public async Task<JsonResult> GetNotifications()
        {
            var userId = User.Identity.GetUserId();
            var unreadCount = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var notificationsFromDb = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var notifications = notificationsFromDb.Select(n => new
            {
                n.Id,
                Message = MemberNames.NotificationText(n.Message),
                n.IsRead,
                CreatedAt = n.CreatedAt.ToString("g"),
                Url = NotificationLinkHelper.Resolve(n, Url)
            }).ToList();

            return Json(new { notifications, unreadCount });
        }

        ////////////////////////////////////////////////////////////////////////////
        // POST: Mark notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var userId = User.Identity.GetUserId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            await HubNotifications.NotifyBadgesAsync(_hubContext, _logger, userId);
            return Json(new { success = true });
        }

        ////////////////////////////////////////////////////////////////////////////
        // POST: Decline skill request
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> DeclineSkillRequest(int notificationId)
        {
            var userId = User.Identity.GetUserId();
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null || notif.Type != "SkillRequest") return Json(new { success = false });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var skillRequest = await _context.SkillRequests.Include(r => r.Skill).Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == notif.ReferenceId && r.ReceiverId == userId);
            if (skillRequest != null && skillRequest.Status == "Pending" && skillRequest.ReceiverId == userId)
            {
                var claimed = await _context.SkillRequests.Where(r => r.Id == skillRequest.Id &&
                        r.ReceiverId == userId && r.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "Declined"));
                if (claimed != 1) return Json(new { success = false });
                skillRequest.Status = "Declined";

                var requesterName = await MemberNames.GetAsync(_context, skillRequest.RequesterId);
                var currentUserName = await MemberNames.GetAsync(_context, userId);

                notif.Type = "RequestUpdate";
                notif.Message = $"You declined the skill request for {skillRequest.Skill.Name} from {requesterName}.";
                notif.IsRead = true;

                _context.Notifications.Add(new Notification
                {
                    UserId = skillRequest.RequesterId,
                    Type = "RequestUpdate",
                    ReferenceId = skillRequest.Id,
                    Message = $"Your skill request for {skillRequest.Skill.Name} was declined by {currentUserName}.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await HubNotifications.NotifyBadgesAsync(_hubContext, _logger, userId, skillRequest.RequesterId);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        ////////////////////////////////////////////////////////////////////////////
        // GET: Accept request → return skills requester can teach that current user wants to learn + 1-way mentorship availability
        [HttpGet]
        public async Task<JsonResult> AcceptSkillRequest(int notificationId)
        {
            var userId = User.Identity.GetUserId();
            var notif = await _context.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null || notif.Type != "SkillRequest")
                return Json(new { skills = Array.Empty<object>(), canAcceptOneWay = false, requesterTeachableCount = 0 });

            var skillRequest = await _context.SkillRequests
                .AsNoTracking()
                .Include(r => r.Skill)
                .FirstOrDefaultAsync(r => r.Id == notif.ReferenceId && r.Status == "Pending" && r.ReceiverId == userId);
            if (skillRequest == null)
                return Json(new { skills = Array.Empty<object>(), canAcceptOneWay = false, requesterTeachableCount = 0 });
            if (await BlockRules.EitherBlockedAsync(_context, userId, skillRequest.RequesterId))
                return Json(new { skills = Array.Empty<object>(), canAcceptOneWay = false, requesterTeachableCount = 0 });

            var requesterId = skillRequest.RequesterId;
            var receiverId = userId;

            var requesterSkills = _context.UserSkills
                .AsNoTracking()
                .Include(us => us.Skill)
                .Where(us => us.UserId == requesterId && us.Status == "Teaching");

            var receiverSkills = _context.UserSkills
                .AsNoTracking()
                .Where(us => us.UserId == receiverId && us.Status == "Learning");

            var matchingSkills = await requesterSkills
                .Where(rs => rs.SkillId != skillRequest.SkillId && rs.KnownUpToStage > 0 &&
                    receiverSkills.Any(ls => ls.SkillId == rs.SkillId))
                .Select(rs => new { rs.SkillId, rs.Skill.Name })
                .ToListAsync();

            var requesterTeachableCount = await requesterSkills
                .CountAsync(rs => rs.SkillId != skillRequest.SkillId && rs.KnownUpToStage > 0);

            return Json(new { skills = matchingSkills, canAcceptOneWay = true, requesterTeachableCount });
        }

        ////////////////////////////////////////////////////////////////////////////
        // POST: Initialize interaction as either a 2-way skill swap (skillId set) or a 1-way mentorship (skillId null)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> InitializeInteraction(int notificationId, int? skillId = null)
        {
            if (skillId.HasValue && skillId.Value < 0)
                return Json(new { success = false });

            int? normalizedSkillId = (skillId.HasValue && skillId.Value > 0) ? skillId.Value : null;

            var userId = User.Identity.GetUserId();
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null || notif.Type != "SkillRequest")
                return Json(new { success = false });

            var skillRequest = await _context.SkillRequests
                .Include(r => r.Skill)
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == notif.ReferenceId && r.Status == "Pending" && r.ReceiverId == userId);

            if (skillRequest == null)
                return Json(new { success = false });
            if (await BlockRules.EitherBlockedAsync(_context, userId, skillRequest.RequesterId))
                return Json(new { success = false });

            var receiverCanTeach = await _context.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == userId &&
                us.SkillId == skillRequest.SkillId && us.Status == "Teaching" && us.KnownUpToStage > 0);
            var requesterWantsToLearn = await _context.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == skillRequest.RequesterId &&
                us.SkillId == skillRequest.SkillId && us.Status == "Learning");
            if (!receiverCanTeach || !requesterWantsToLearn)
                return Json(new { success = false });

            if (normalizedSkillId.HasValue)
            {
                var reciprocalSkillId = normalizedSkillId.Value;
                var requesterCanTeach = await _context.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == skillRequest.RequesterId &&
                    us.SkillId == reciprocalSkillId && us.Status == "Teaching" && us.KnownUpToStage > 0);
                var receiverWantsToLearn = await _context.UserSkills.AsNoTracking().AnyAsync(us => us.UserId == userId &&
                    us.SkillId == reciprocalSkillId && us.Status == "Learning");
                if (reciprocalSkillId == skillRequest.SkillId || !requesterCanTeach || !receiverWantsToLearn)
                    return Json(new { success = false });
            }

            int interactionId;
            await using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var claimed = await _context.SkillRequests.Where(r => r.Id == skillRequest.Id &&
                        r.ReceiverId == userId && r.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "Accepted"));
                if (claimed != 1) return Json(new { success = false });

                var interaction = new Interaction
                {
                    User1Id = userId,
                    User2Id = skillRequest.RequesterId,
                    SkillFromTeacherId = skillRequest.SkillId,
                    SkillFromRequesterId = normalizedSkillId,
                    Status = "Ongoing",
                    CreatedAt = DateTime.Now
                };
                _context.Interactions.Add(interaction);
                await _context.SaveChangesAsync();
                interactionId = interaction.Id;

                if (interaction.SkillFromRequesterId.HasValue)
                {
                    var requesterSkillId = interaction.SkillFromRequesterId.Value;
                    var requesterSkill = await _context.UserSkills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(us => us.UserId == interaction.User2Id && us.SkillId == requesterSkillId && us.Status == "Teaching");

                    int maxRequesterStage = requesterSkill?.KnownUpToStage ?? 0;

                    var requesterStages = await _context.SkillStages
                        .AsNoTracking()
                        .Where(ss => ss.SkillId == requesterSkillId && ss.StageNumber <= maxRequesterStage)
                        .OrderBy(ss => ss.StageNumber)
                        .ToListAsync();

                    foreach (var stage in requesterStages)
                    {
                        _context.InteractionSessions.Add(new InteractionSession
                        {
                            InteractionId = interaction.Id,
                            SkillId = stage.SkillId,
                            StageNumber = stage.StageNumber,
                            Title = stage.Description,
                            CatalogStageNumber = stage.StageNumber,
                            Status = "Pending",
                            User1Confirmed = false,
                            User2Confirmed = false
                        });
                    }
                }

                var teacherSkill = await _context.UserSkills
                    .AsNoTracking()
                    .FirstOrDefaultAsync(us => us.UserId == interaction.User1Id && us.SkillId == interaction.SkillFromTeacherId && us.Status == "Teaching");

                int maxTeacherStage = teacherSkill?.KnownUpToStage ?? 0;

                var teacherStages = await _context.SkillStages
                    .AsNoTracking()
                    .Where(ss => ss.SkillId == interaction.SkillFromTeacherId && ss.StageNumber <= maxTeacherStage)
                    .OrderBy(ss => ss.StageNumber)
                    .ToListAsync();

                foreach (var stage in teacherStages)
                {
                    _context.InteractionSessions.Add(new InteractionSession
                    {
                        InteractionId = interaction.Id,
                        SkillId = stage.SkillId,
                        StageNumber = stage.StageNumber,
                        Title = stage.Description,
                        CatalogStageNumber = stage.StageNumber,
                        Status = "Pending",
                        User1Confirmed = false,
                        User2Confirmed = false
                    });
                }

                skillRequest.Status = "Accepted";

                var requesterName = await MemberNames.GetAsync(_context, skillRequest.RequesterId);
                var currentUserName = await MemberNames.GetAsync(_context, userId);
                var mentorshipLabel = normalizedSkillId.HasValue ? "" : " as a 1-way mentorship";

                notif.Type = "Exchange";
                notif.ReferenceId = interaction.Id;
                notif.Message = $"You accepted the skill request for {skillRequest.Skill.Name} from {requesterName}{mentorshipLabel}.";
                notif.IsRead = true;

                _context.Notifications.Add(new Notification
                {
                    UserId = skillRequest.RequesterId,
                    Type = "Exchange",
                    ReferenceId = interaction.Id,
                    Message = $"Your skill request for {skillRequest.Skill.Name} has been accepted by {currentUserName}{mentorshipLabel}.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            await HubNotifications.NotifyBadgesAsync(_hubContext, _logger, userId, skillRequest.RequesterId);
            return Json(new { success = true, interactionId, isOneWay = !normalizedSkillId.HasValue });
        }

        ////////////////////////////////////////////////////////////////////////////
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> MarkAllAsRead()
        {
            var userId = User.Identity.GetUserId();
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            await HubNotifications.NotifyBadgesAsync(_hubContext, _logger, userId);
            return Json(new { success = true });
        }

        ////////////////////////////////////////////////////////////////////////////
        [HttpGet]
        public async Task<JsonResult> GetRealtimeNotifications()
        {
            var userId = User.Identity.GetUserId();
            var unreadCount = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var notificationsFromDb = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var notifications = notificationsFromDb.Select(n => new
            {
                n.Id,
                Message = MemberNames.NotificationText(n.Message),
                n.IsRead,
                n.Type,
                CreatedAt = n.CreatedAt.ToString("g"),
                Url = NotificationLinkHelper.Resolve(n, Url)
            }).ToList();

            return Json(new { notifications, unreadCount });
        }
    }
}
