using System.Collections.Generic;

namespace SkillBridge.Models
{
    public class PublicProfileViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Profession { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string AvailabilityNotes { get; set; }
        public int AvailableDaysMask { get; set; }
        public string TimeZoneId { get; set; }
        public string MeetingFormat { get; set; }
        public bool IsSaved { get; set; }

        public List<SkillViewModel> SkillsToTeach { get; set; }
        public List<SkillViewModel> SkillsToLearn { get; set; }


        public double AverageRating { get; set; }
        public int RatingsReceived { get; set; }
        public int InteractionsCompleted { get; set; }

        public string ProfileImageUrl { get; set; }
        public string YouCanLearn { get; set; } = "";
        public string TheyCanLearn { get; set; } = "";
    }

    public class SkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int Stage { get; set; }
        public int TotalStages { get; set; }

        public string RequestStatus { get; set; } = "None"; // None | Pending | Declined

        public int UserSkillId { get; set; }
    }

    public class GuestExploreViewModel
    {
        public string Query { get; set; } = "";
        public List<GuestSkillViewModel> Skills { get; set; } = new();
        public List<SkillSuggestion> MySuggestions { get; set; } = new();
        public bool CanReviewSuggestions { get; set; }
    }

    public class GuestSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string CategoryName { get; set; }
        public List<GuestTeacherViewModel> Teachers { get; set; } = new();
    }

    public class SkillDetailViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public List<SkillStage> Stages { get; set; } = new();
        public List<GuestTeacherViewModel> Teachers { get; set; } = new();
        public int? CommunityId { get; set; }
    }

    public class GuestTeacherViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
    }

    public class SavedProfileItemViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Profession { get; set; }
        public bool CanView { get; set; }
        public string MatchSummary { get; set; }
    }
}
