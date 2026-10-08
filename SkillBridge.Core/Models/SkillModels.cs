using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillBridge.Models
{
    public class SkillCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public virtual ICollection<Skill> Skills { get; set; }
    }


    public class Skill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public int SkillCategoryId { get; set; }

        [ForeignKey("SkillCategoryId")]
        public virtual SkillCategory SkillCategory { get; set; }

        public virtual ICollection<SkillStage> SkillStages { get; set; }
    }


    public class SkillStage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 7)]
        public int StageNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        public int SkillId { get; set; }

        [ForeignKey("SkillId")]
        public virtual Skill Skill { get; set; }
    }
}
