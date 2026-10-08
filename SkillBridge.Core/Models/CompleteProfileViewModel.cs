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

        [Required(ErrorMessage = "Age is required.")]
        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Profession is required.")]
        [StringLength(100)]
        public string Profession { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(100)]
        public string Location { get; set; }

        [Required(ErrorMessage = "Bio is required.")]
        [StringLength(500)]
        public string Bio { get; set; }

        [Required(ErrorMessage = "Select at least one skill to learn.")]
        public List<int> SkillsToLearn { get; set; } = new List<int>(); 

        [Required(ErrorMessage = "Select at least one skill you know.")]
        public List<UserKnownSkill> SkillsIKnow { get; set; } = new List<UserKnownSkill>();

        public List<SkillCategory> AllSkillCategories { get; set; } = new List<SkillCategory>();


        public class UserKnownSkill
        {
            public int SkillId { get; set; }
            [Required]
            public int KnownUpToStage { get; set; }
        }
    }
}
