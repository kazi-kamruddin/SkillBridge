using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models
{
    public class CompleteProfileViewModel
    {
        public bool IsPublic { get; set; }
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150.")]
        public int? Age { get; set; }

        [StringLength(100)]
        public string Profession { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        public List<int> SkillsToLearn { get; set; } = new List<int>();

        public List<UserKnownSkill> SkillsIKnow { get; set; } = new List<UserKnownSkill>();
        public Microsoft.AspNetCore.Http.IFormFile ProfileImage { get; set; }

        public List<SkillCategory> AllSkillCategories { get; set; } = new List<SkillCategory>();


        public class UserKnownSkill
        {
            public int SkillId { get; set; }
            [Required]
            public int KnownUpToStage { get; set; }
        }
    }
}
