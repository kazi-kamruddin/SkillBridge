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
        public string EndReason { get; set; }
    }

    public class SkillStageBlock
    {
        public int SessionId { get; set; }
        public int StageNumber { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Red, Yellow, Green
        public bool UserConfirmed { get; set; }
        public bool IsLocked { get; set; }
        public bool IsEditable { get; set; }
        public List<StageNoteViewModel> Notes { get; set; } = new();
    }

    public class StageNoteViewModel
    {
        public bool IsMine { get; set; }
        public string WhatWeCovered { get; set; }
        public string NextStep { get; set; }
        public System.DateTime UpdatedAt { get; set; }
    }


    public class InteractionSessionsViewModel
    {
        public int InteractionId { get; set; }
        public string UserId { get; set; }
        public List<SkillStageBlock> SkillBlocks { get; set; }
        public System.DateTime? MeetingStartUtc { get; set; }
        public string MeetingFormat { get; set; }
        public string MeetingNote { get; set; }
        public string MeetingStatus { get; set; }
        public bool CanRespondToMeeting { get; set; }
        public List<ExchangePlanProposalViewModel> PlanProposals { get; set; } = new();
    }

    public class ExchangePlanProposalViewModel
    {
        public int SkillId { get; set; }
        public bool ProposedByMe { get; set; }
        public List<PlanStepInput> Steps { get; set; } = new();
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
