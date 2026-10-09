using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models;

public sealed class SavedProfile
{
    [Required, StringLength(128)] public string UserId { get; set; }
    [Required, StringLength(128)] public string TargetUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ApplicationUser User { get; set; }
    public ApplicationUser TargetUser { get; set; }
}

public sealed class InteractionSessionNote
{
    public int Id { get; set; }
    public int InteractionSessionId { get; set; }
    [Required, StringLength(128)] public string UserId { get; set; }
    [Required, StringLength(1000)] public string WhatWeCovered { get; set; }
    [StringLength(500)] public string NextStep { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public InteractionSession InteractionSession { get; set; }
    public ApplicationUser User { get; set; }
}
