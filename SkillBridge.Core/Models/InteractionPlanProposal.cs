using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models;

public class InteractionPlanProposal
{
    public int InteractionId { get; set; }
    public int SkillId { get; set; }
    [Required, StringLength(128)] public string ProposedByUserId { get; set; }
    [Required] public string StepsJson { get; set; }
    [Required] public string OriginalStepsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Interaction Interaction { get; set; }
    public Skill Skill { get; set; }
}

public class PlanStepInput
{
    public int SessionId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; }
}

public class PlanStepSnapshot
{
    public int SessionId { get; set; }
    public int StageNumber { get; set; }
    public string Title { get; set; }
    public bool User1Confirmed { get; set; }
    public bool User2Confirmed { get; set; }
}
