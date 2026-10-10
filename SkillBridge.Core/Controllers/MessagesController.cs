using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using SkillBridge.Helpers;
using SkillBridge.Models;
using SkillBridge.Services;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SkillBridge.Hubs.ChatHub> hubContext;
        private readonly CloudinaryImageService images;

        public MessagesController(ApplicationDbContext db,
            Microsoft.AspNetCore.SignalR.IHubContext<SkillBridge.Hubs.ChatHub> hubContext, CloudinaryImageService images)
        {
            this.db = db;
            this.hubContext = hubContext;
            this.images = images;
        }

        // GET: /Messages
        // List all conversations for current user
        [Authorize]
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();

            var firstConversation = await db.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Where(c => !db.MemberBlocks.Any(b =>
                    (b.BlockerId == c.User1Id && b.BlockedId == c.User2Id) ||
                    (b.BlockerId == c.User2Id && b.BlockedId == c.User1Id)))
                .OrderByDescending(c => c.LastMessageAt)
                .FirstOrDefaultAsync();

            if (firstConversation != null)
            {
                return RedirectToAction("Chat", new { id = firstConversation.Id });
            }

            ViewBag.Message = "You don’t have any conversations yet. Go to Explore and Knock someone!";
            return View("NoConversations");
        }


        // GET: /Messages/Chat/5
        // Show one conversation (with decrypted messages + sidebar lists)
        public async Task<ActionResult> Chat(int id)
        {
            var userId = User.Identity.GetUserId();

            var conversation = await db.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id &&
                                           (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null) return NotFound();
            var otherId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
            if (BlockRules.EitherBlocked(db, userId, otherId)) return StatusCode(403);

            var decryptedMessages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    FromUserId = m.FromUserId,
                    ToUserId = m.ToUserId,
                    Text = MessageEncryptionService.Decrypt(m.Ciphertext, m.IV, m.Hmac),
                    HasImage = !string.IsNullOrWhiteSpace(m.ImagePublicId),
                    CreatedAt = m.CreatedAt,
                    IsMine = (m.FromUserId == userId),
                    IsRead = m.IsRead
                })
                .ToList();

            ViewBag.Messages = decryptedMessages;
            ViewBag.ConversationId = id;
            ViewBag.OtherUserId = conversation.User1Id == userId
                ? conversation.User2Id
                : conversation.User1Id;

            var allConversations = await db.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Where(c => !db.MemberBlocks.Any(b =>
                    (b.BlockerId == c.User1Id && b.BlockedId == c.User2Id) ||
                    (b.BlockerId == c.User2Id && b.BlockedId == c.User1Id)))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
            ViewBag.AllConversations = allConversations;
            var conversationIds = allConversations.Select(c => c.Id).ToList();
            var latestMessages = await db.Messages.Where(m => conversationIds.Contains(m.ConversationId))
                .GroupBy(m => m.ConversationId)
                .Select(group => group.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).First())
                .ToListAsync();
            ViewBag.LastMessagePreviews = latestMessages.ToDictionary(m => m.ConversationId, m =>
            {
                var text = MessageEncryptionService.Decrypt(m.Ciphertext, m.IV, m.Hmac);
                return string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(m.ImagePublicId)
                    ? "Image" : text;
            });
            ViewBag.UnreadByConversation = await db.Messages
                .Where(m => conversationIds.Contains(m.ConversationId) && m.ToUserId == userId && !m.IsRead)
                .GroupBy(m => m.ConversationId)
                .Select(group => new { ConversationId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.ConversationId, x => x.Count);
            var partnerIds = allConversations.Select(c => c.User1Id == userId ? c.User2Id : c.User1Id).Distinct().ToList();
            ViewBag.PartnerNames = await db.UserInformations.Where(info => partnerIds.Contains(info.UserId))
                .ToDictionaryAsync(info => info.UserId, info => info.FullName);

            var partnerId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
            var profileInfo = await db.UserInformations.FirstOrDefaultAsync(u => u.UserId == partnerId);

            var partnerVm = new ChatPartnerViewModel
            {
                FullName = profileInfo?.FullName ?? "Unknown",
                Profession = profileInfo?.Profession ?? "",
                Location = profileInfo?.Location ?? "",
                Bio = profileInfo?.Bio ?? "",
                ProfileImageUrl = ProfileImageHelper.GetProfileImage(profileInfo?.ProfileImageUrl, profileInfo?.FullName)
            };

            ViewBag.OtherUserProfile = partnerVm;

            return View(conversation);
        }




        // POST: /Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult> Send(int conversationId, string messageText, IFormFile image)
        {
            messageText ??= "";
            if ((string.IsNullOrWhiteSpace(messageText) && image?.Length is not > 0) || messageText.Length > 4000)
                return StatusCode(400, "Add a message or an image; text can be at most 4000 characters.");
            if (image?.Length > 0 && string.IsNullOrWhiteSpace(messageText)) messageText = "";
            var imageError = CloudinaryImageService.Validate(image);
            if (imageError != null) return BadRequest(imageError);

            var userId = User.Identity.GetUserId();

            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                           (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null) return NotFound();

            var otherUserId = (conversation.User1Id == userId) ? conversation.User2Id : conversation.User1Id;
            if (BlockRules.EitherBlocked(db, userId, otherUserId)) return StatusCode(403);

            UploadedImage uploaded = null;
            if (image?.Length > 0)
            {
                try { uploaded = await images.UploadAsync(image, "skillbridge/chat", true); }
                catch { return StatusCode(502, "The image could not be uploaded. Please try again."); }
            }

            var encrypted = MessageEncryptionService.Encrypt(messageText);

            var msg = new Message
            {
                ConversationId = conversation.Id,
                FromUserId = userId,
                ToUserId = otherUserId,
                Ciphertext = encrypted.Ciphertext,
                IV = encrypted.IV,
                Hmac = encrypted.Hmac,
                ImagePublicId = uploaded?.PublicId,
                ImageFormat = uploaded?.Format,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            db.Messages.Add(msg);
            conversation.LastMessageAt = DateTime.Now;
            await db.SaveChangesAsync();

            var groupName = $"convo-{conversation.Id}";

            await hubContext.Clients.Group(groupName).SendAsync("receiveMessage", new
            {
                id = msg.Id,
                conversationId = conversation.Id,
                fromUserId = msg.FromUserId,
                toUserId = msg.ToUserId,
                text = messageText,
                hasImage = uploaded != null,
                sentAt = msg.CreatedAt.ToString("o")
            });

            return StatusCode(200);
        }

        [HttpGet]
        public async Task<IActionResult> Image(int id)
        {
            var userId = User.Identity.GetUserId();
            var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == id && m.ImagePublicId != null);
            if (message == null) return NotFound();
            var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == message.ConversationId &&
                (c.User1Id == userId || c.User2Id == userId));
            if (conversation == null) return NotFound();
            var otherId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
            if (BlockRules.EitherBlocked(db, userId, otherId)) return StatusCode(403);
            try
            {
                var bytes = await images.DownloadAuthenticatedAsync(message.ImagePublicId, message.ImageFormat);
                Response.Headers.CacheControl = "private, no-store";
                var contentType = message.ImageFormat?.ToLowerInvariant() switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "png" => "image/png",
                    "webp" => "image/webp",
                    _ => "application/octet-stream"
                };
                return File(bytes, contentType);
            }
            catch { return StatusCode(502, "The image is temporarily unavailable."); }
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = User.Identity.GetUserId();
            var count = await db.Messages.CountAsync(m => m.ToUserId == userId && !m.IsRead &&
                !db.MemberBlocks.Any(b =>
                    (b.BlockerId == m.FromUserId && b.BlockedId == userId) ||
                    (b.BlockedId == m.FromUserId && b.BlockerId == userId)));
            return Json(new { count });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<IActionResult> MarkRead(int conversationId)
        {
            var userId = User.Identity.GetUserId();
            var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.User1Id == userId || c.User2Id == userId));
            if (conversation == null) return NotFound();
            var otherId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
            if (BlockRules.EitherBlocked(db, userId, otherId)) return StatusCode(403);
            var unread = await db.Messages.Where(m => m.ConversationId == conversationId &&
                m.ToUserId == userId && !m.IsRead).ToListAsync();
            if (unread.Count > 0)
            {
                foreach (var message in unread) message.IsRead = true;
                await db.SaveChangesAsync();
                await hubContext.Clients.Group($"convo-{conversationId}").SendAsync("messagesRead", new
                {
                    conversationId,
                    readerId = userId,
                    messageIds = unread.Select(m => m.Id).ToArray()
                });
            }
            return Json(new { count = unread.Count });
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> Knock(string targetUserId)
        {
            var currentUserId = User.Identity.GetUserId();

            if (string.IsNullOrWhiteSpace(targetUserId) ||
                !await db.Users.AnyAsync(u => u.Id == targetUserId))
                return NotFound();

            if (targetUserId == currentUserId)
                return RedirectToAction("Index");
            if (!await db.UserSkills.AnyAsync(s => s.UserId == currentUserId && s.Status == "Teaching") ||
                !await db.UserSkills.AnyAsync(s => s.UserId == currentUserId && s.Status == "Learning"))
            {
                TempData["AccountNotice"] = "Add a skill you can teach and one you want to learn before starting a conversation.";
                return RedirectToAction("UpdateProfile", "Profile");
            }
            if (!await db.UserSkills.AnyAsync(s => s.UserId == targetUserId && s.Status == "Teaching") ||
                !await db.UserSkills.AnyAsync(s => s.UserId == targetUserId && s.Status == "Learning"))
            {
                TempData["ProfileNotice"] = "This member is still setting up their skills.";
                return RedirectToAction("PublicProfile", "Profile", new { id = targetUserId });
            }
            if (BlockRules.EitherBlocked(db, currentUserId, targetUserId)) return StatusCode(403);

            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == currentUserId && c.User2Id == targetUserId) ||
                    (c.User1Id == targetUserId && c.User2Id == currentUserId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = currentUserId,
                    User2Id = targetUserId,
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now
                };

                db.Conversations.Add(conversation);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Chat", new { id = conversation.Id });
        }

    }
}
