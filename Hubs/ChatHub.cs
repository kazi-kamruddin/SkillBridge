using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace SkillBridge.Hubs
{
    public class ChatHub : Hub
    {
        public Task JoinConversation(string conversationGroup)
        {
            return Groups.Add(Context.ConnectionId, conversationGroup);
        }

        public Task LeaveConversation(string conversationGroup)
        {
            return Groups.Remove(Context.ConnectionId, conversationGroup);
        }
    }
}
