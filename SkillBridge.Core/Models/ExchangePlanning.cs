using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models;

public class SavedProfile
{
    [Required, StringLength(128)] public string UserId { get; set; }
    [Required, StringLength(128)] public string TargetUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public virtual ApplicationUser User { get; set; }
    public virtual ApplicationUser TargetUser { get; set; }
}

public class InteractionSessionNote
{
    public int Id { get; set; }
    public int InteractionSessionId { get; set; }
    [Required, StringLength(128)] public string UserId { get; set; }
    [Required, StringLength(1000)] public string WhatWeCovered { get; set; }
    [StringLength(500)] public string NextStep { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public virtual InteractionSession InteractionSession { get; set; }
    public virtual ApplicationUser User { get; set; }
}

public class InteractionMeetingEvent
{
    public int Id { get; set; }
    public int InteractionId { get; set; }
    [Required, StringLength(24)] public string EventType { get; set; }
    public DateTime StartsAtUtc { get; set; }
    [StringLength(20)] public string Format { get; set; }
    [StringLength(300)] public string Note { get; set; }
    [StringLength(128)] public string ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public virtual Interaction Interaction { get; set; }
}
