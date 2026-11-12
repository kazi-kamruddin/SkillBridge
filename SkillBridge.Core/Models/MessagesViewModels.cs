using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillBridge.Models
{
    public class ChatPartnerViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Profession { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string ProfileImageUrl { get; set; }
    }

    public class InboxConversationViewModel
    {
        public int ConversationId { get; set; }
        public string PartnerUserId { get; set; }
        public string PartnerName { get; set; }
        public string PartnerProfession { get; set; }
        public string PartnerAvatarUrl { get; set; }
        public string LastMessagePreview { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageViewModel
    {
        public int Id { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string Text { get; set; }
        public bool HasImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMine { get; set; }
        public bool IsRead { get; set; }
    }
}
