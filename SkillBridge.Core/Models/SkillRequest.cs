using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillBridge.Models
{
    public class SkillRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; } 

        [Required]
        public string ReceiverId { get; set; } 

        [Required]
        public int SkillId { get; set; } 

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // "Pending", "Accepted", "Declined"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RequesterId")]
        public virtual ApplicationUser Requester { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; }

        [ForeignKey("SkillId")]
        public virtual Skill Skill { get; set; }
    }
}
