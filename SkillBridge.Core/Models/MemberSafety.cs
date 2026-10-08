using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models;

public class MemberBlock
{
    public int Id { get; set; }
    [Required] public string BlockerId { get; set; }
    [Required] public string BlockedId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ProfileReport
{
    public int Id { get; set; }
    [Required] public string ReporterId { get; set; }
    [Required] public string ReportedUserId { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
