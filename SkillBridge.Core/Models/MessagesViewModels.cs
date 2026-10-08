using System;
using System.Collections.Generic;
using System.Linq;


namespace SkillBridge.Models
{
    public class ChatPartnerViewModel
    {
        public string FullName { get; set; }
        public string Profession { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string ProfileImageUrl { get; set; }
    }

    public class ChatMessageViewModel
    {
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMine { get; set; }
    }
}