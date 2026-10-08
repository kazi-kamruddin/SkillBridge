using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using SkillBridge.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SkillBridge.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public Task JoinConversation(int conversationId)
        {
            EnsureParticipant(conversationId);
            return Groups.Add(Context.ConnectionId, "convo-" + conversationId);
        }

        public Task LeaveConversation(int conversationId)
        {
            EnsureParticipant(conversationId);
            return Groups.Remove(Context.ConnectionId, "convo-" + conversationId);
        }

        private void EnsureParticipant(int conversationId)
        {
            var userId = Context.User?.Identity.GetUserId();
            using (var db = new ApplicationDbContext())
            {
                var allowed = userId != null && db.Conversations.Any(c =>
                    c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId));
                if (!allowed)
                    throw new HubException("Conversation not found.");
            }
        }
    }
}
