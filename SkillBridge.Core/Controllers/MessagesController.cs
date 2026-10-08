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

        public MessagesController(ApplicationDbContext db,
            Microsoft.AspNetCore.SignalR.IHubContext<SkillBridge.Hubs.ChatHub> hubContext)
        {
            this.db = db;
            this.hubContext = hubContext;
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
                    FromUserId = m.FromUserId,
                    ToUserId = m.ToUserId,
                    Text = MessageEncryptionService.Decrypt(m.Ciphertext, m.IV, m.Hmac),
                    CreatedAt = m.CreatedAt,
                    IsMine = (m.FromUserId == userId)
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

            var partnerId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
            var profileInfo = await db.UserInformations.FirstOrDefaultAsync(u => u.UserId == partnerId);

            var partnerVm = new ChatPartnerViewModel
            {
                FullName = profileInfo?.FullName ?? "Unknown",
                Profession = profileInfo?.Profession ?? "",
                Location = profileInfo?.Location ?? "",
                Bio = profileInfo?.Bio ?? "",
                ProfileImageUrl = ProfileImageHelper.GetRandomProfileImage()
            };

            ViewBag.OtherUserProfile = partnerVm;

            return View(conversation);
        }




        // POST: /Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> Send(int conversationId, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText) || messageText.Length > 4000)
                return StatusCode(400, "Message must be 1 to 4000 characters long.");

            var userId = User.Identity.GetUserId();

            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                           (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null) return NotFound();

            var otherUserId = (conversation.User1Id == userId) ? conversation.User2Id : conversation.User1Id;
            if (BlockRules.EitherBlocked(db, userId, otherUserId)) return StatusCode(403);

            var encrypted = MessageEncryptionService.Encrypt(messageText);

            var msg = new Message
            {
                ConversationId = conversation.Id,
                FromUserId = userId,
                ToUserId = otherUserId,
                Ciphertext = encrypted.Ciphertext,
                IV = encrypted.IV,
                Hmac = encrypted.Hmac,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            db.Messages.Add(msg);
            conversation.LastMessageAt = DateTime.Now;
            await db.SaveChangesAsync();

            var groupName = $"convo-{conversation.Id}";

            await hubContext.Clients.Group(groupName).SendAsync("receiveMessage", new
            {
                conversationId = conversation.Id,
                fromUserId = msg.FromUserId,
                toUserId = msg.ToUserId,
                text = messageText,         
                sentAt = msg.CreatedAt.ToString("o")
            });

            return StatusCode(200); 
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
