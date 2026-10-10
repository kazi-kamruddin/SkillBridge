using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models;

public class SkillSuggestion
{
    public int Id { get; set; }
    [Required, StringLength(128)] public string UserId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; }
    [Required, StringLength(100)] public string CategoryName { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
