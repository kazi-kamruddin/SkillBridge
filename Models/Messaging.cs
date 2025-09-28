using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillBridge.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        [Required, StringLength(128)]
        [Index("IX_Convo_UserPair", 0, IsUnique = true)]
        public string User1Id { get; set; }

        [Required, StringLength(128)]
        [Index("IX_Convo_UserPair", 1, IsUnique = true)]
        public string User2Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastMessageAt { get; set; }

        public virtual ApplicationUser User1 { get; set; }
        public virtual ApplicationUser User2 { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }

        [Index("IX_Messages_Conversation_CreatedAt", 0)]
        public int ConversationId { get; set; }

        [Required, StringLength(128)]
        public string FromUserId { get; set; }

        // Composite index with IsRead for fast “unread messages” lookup
        [Required, StringLength(128)]
        [Index("IX_Messages_ToUser_IsRead", 0)]
        public string ToUserId { get; set; }

        // --- Encrypted payload (AES-CBC + HMAC) ---
        [Required] public byte[] Ciphertext { get; set; }   // encrypted message bytes
        [Required] public byte[] IV { get; set; }           // AES Initialization Vector
        [Required] public byte[] Hmac { get; set; }         // HMAC for tamper detection

        [Index("IX_Messages_Conversation_CreatedAt", 1)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Index("IX_Messages_ToUser_IsRead", 1)]
        public bool IsRead { get; set; } = false;

        public virtual Conversation Conversation { get; set; }
        public virtual ApplicationUser FromUser { get; set; }
        public virtual ApplicationUser ToUser { get; set; }
    }

}
