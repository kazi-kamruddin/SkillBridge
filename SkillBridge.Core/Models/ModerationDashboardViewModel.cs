namespace SkillBridge.Models;

public class ModerationDashboardViewModel
{
    public List<ModerationReportViewModel> CommunityReports { get; set; } = new();
    public List<ProfileReportItemViewModel> ProfileReports { get; set; } = new();
    public List<HiddenProfileViewModel> HiddenProfiles { get; set; } = new();
}

public class HiddenProfileViewModel
{
    public string UserId { get; set; }
    public string Name { get; set; }
}

public class ProfileReportItemViewModel
{
    public int Id { get; set; }
    public string ReportedUserId { get; set; }
    public string ReportedName { get; set; }
    public string ProfileBio { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
