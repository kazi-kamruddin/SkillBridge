using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models
{
    public class InteractionIndexViewModel
    {
        public int InteractionId { get; set; }
        public string OtherUserName { get; set; }
        public string SkillYouTeach { get; set; }
        public string SkillYouLearn { get; set; }
        public string Status { get; set; }
    }

    public class SkillStageBlock
    {
        public int StageNumber { get; set; }
        public int SkillId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Red, Yellow, Green
        public bool UserConfirmed { get; set; }
        public bool IsLocked { get; set; } 
    }


    public class InteractionSessionsViewModel
    {
        public int InteractionId { get; set; }
        public string UserId { get; set; }
        public List<SkillStageBlock> SkillBlocks { get; set; }
    }

    public class InteractionRatingViewModel
    {
        public int InteractionId { get; set; }
        public string SkillName { get; set; }
        public string FromUserName { get; set; }
        public string ToUserId { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public int RatingValue { get; set; }

        [StringLength(250)]
        public string Comment { get; set; }
    }

    public class InteractionFeedbackViewModel
    {
        public InteractionRatingViewModel RatingModel { get; set; }
        public InteractionIndexViewModel IndexModel { get; set; }
    }


}
