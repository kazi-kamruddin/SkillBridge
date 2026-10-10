using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillBridge.Models
{
    public class UserInformation
    {
        [Key]
        [ForeignKey("User")]
        public string UserId { get; set; } 

        [Required]
        [StringLength(150)]
        public string FullName { get; set; }

        public int? Age { get; set; }

        [StringLength(100)]
        public string Profession { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        [StringLength(500)]
        public string ProfileImageUrl { get; set; }
        [StringLength(500)]
        public string ProfileImagePublicId { get; set; }

        public bool IsPublic { get; set; }
        public bool IsHidden { get; set; }

        [StringLength(300)]
        public string AvailabilityNotes { get; set; }

        [StringLength(20)]
        public string MeetingFormat { get; set; } = "Either";

        public virtual ApplicationUser User { get; set; }
    }

    public class UserSkill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; } 
        public virtual ApplicationUser User { get; set; }

        [Required]
        [ForeignKey("Skill")]
        public int SkillId { get; set; } 
        public virtual Skill Skill { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; } 

        public int? KnownUpToStage { get; set; } 
    }
}
