using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SkillBridge.Helpers;
using SkillBridge.Models;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: /Messages
        // List all conversations for current user
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();

            var conversations = await db.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return View(conversations);
        }

        // GET: /Messages/Chat/5
        // Show one conversation (with decrypted messages)
        public async Task<ActionResult> Chat(int id)
        {
            var userId = User.Identity.GetUserId();

            var conversation = await db.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id &&
                                           (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null) return HttpNotFound();

            // Decrypt all messages for display
            var decryptedMessages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
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
            ViewBag.OtherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

            return View(conversation);
        }

        // POST: /Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Send(int conversationId, string messageText)
        {
            var userId = User.Identity.GetUserId();

            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                           (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null) return HttpNotFound();

            var otherUserId = (conversation.User1Id == userId) ? conversation.User2Id : conversation.User1Id;

            // Encrypt message before saving
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

            return RedirectToAction("Chat", new { id = conversationId });
        }
    }
}
