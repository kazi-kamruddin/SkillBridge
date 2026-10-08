using SkillBridge.Models;

public class UserRating
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int InteractionsCompleted { get; set; } = 0;
    public int RatingsReceived { get; set; } = 0;
    public int AccumulatedRating { get; set; } = 0;

    public virtual ApplicationUser User { get; set; }
}
