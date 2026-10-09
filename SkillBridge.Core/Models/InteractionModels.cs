using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillBridge.Models
{
    public class Interaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string User1Id { get; set; }
        [ForeignKey("User1Id")]
        public virtual ApplicationUser User1 { get; set; }

        [Required]
        public string User2Id { get; set; }
        [ForeignKey("User2Id")]
        public virtual ApplicationUser User2 { get; set; }

        [Required]
        public int SkillFromTeacherId { get; set; }
        [ForeignKey("SkillFromTeacherId")]
        public virtual Skill SkillFromTeacher { get; set; }

        [Required]
        public int SkillFromRequesterId { get; set; }
        [ForeignKey("SkillFromRequesterId")]
        public virtual Skill SkillFromRequester { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [StringLength(500)] public string EndReason { get; set; }
        public string EndedByUserId { get; set; }
        public DateTime? EndedAt { get; set; }

        // Stored as UTC in PostgreSQL's timestamp-without-time-zone column.
        public DateTime? MeetingStartUtc { get; set; }
        [StringLength(20)] public string MeetingFormat { get; set; }
        [StringLength(300)] public string MeetingNote { get; set; }
        public string MeetingProposedByUserId { get; set; }
        [StringLength(20)] public string MeetingStatus { get; set; }

        public virtual ICollection<InteractionSession> Sessions { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
    }

    public class InteractionSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InteractionId { get; set; }
        [ForeignKey("InteractionId")]
        public virtual Interaction Interaction { get; set; }

        [Required]
        public int SkillId { get; set; }
        [ForeignKey("SkillId")]
        public virtual Skill Skill { get; set; }

        [Required]
        [Range(1, 7)]
        public int StageNumber { get; set; }

        public bool User1Confirmed { get; set; } = false;
        public bool User2Confirmed { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
    }

    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InteractionId { get; set; }
        [ForeignKey("InteractionId")]
        public virtual Interaction Interaction { get; set; }

        [Required]
        public string FromUserId { get; set; }
        [ForeignKey("FromUserId")]
        public virtual ApplicationUser FromUser { get; set; }

        [Required]
        public string ToUserId { get; set; }
        [ForeignKey("ToUserId")]
        public virtual ApplicationUser ToUser { get; set; }

        [Required]
        [Range(1, 10)]
        public int RatingValue { get; set; }

        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        public int? ReferenceId { get; set; }
        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
