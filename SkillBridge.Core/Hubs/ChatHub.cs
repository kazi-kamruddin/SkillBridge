using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Models;

namespace SkillBridge.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly ApplicationDbContext db;

    public ChatHub(ApplicationDbContext db) => this.db = db;

    public async Task JoinConversation(int conversationId)
    {
        await EnsureParticipant(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "convo-" + conversationId);
    }

    public async Task LeaveConversation(int conversationId)
    {
        await EnsureParticipant(conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "convo-" + conversationId);
    }

    private async Task EnsureParticipant(int conversationId)
    {
        var userId = Context.User?.Identity.GetUserId();
        var allowed = userId != null && await db.Conversations.AnyAsync(c =>
            c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId) &&
            !db.MemberBlocks.Any(b =>
                (b.BlockerId == c.User1Id && b.BlockedId == c.User2Id) ||
                (b.BlockerId == c.User2Id && b.BlockedId == c.User1Id)));
        if (!allowed)
            throw new HubException("Conversation not found.");
    }
}
