using SkillBridge.Models;
using System.Collections.Generic;
using System.Linq;

namespace SkillBridge.Helpers;

public static class ExchangeProgressHelper
{
    // Custom milestones (CatalogStageNumber == null) never raise verified catalog mastery.
    public static int MaxStageCompleted(IEnumerable<InteractionSession> sessions, int skillId)
    {
        if (sessions == null || skillId <= 0) return 0;

        var completedCatalogStages = sessions
            .Where(s => s.SkillId == skillId && s.User1Confirmed && s.User2Confirmed && s.CatalogStageNumber.HasValue)
            .Select(s => s.CatalogStageNumber!.Value)
            .Where(stage => stage > 0)
            .ToHashSet();
        if (completedCatalogStages.Count == 0) return 0;

        var level = 0;
        while (completedCatalogStages.Contains(level + 1)) level++;
        return level;
    }

    public static (string Learn, string Teach) FormatExchangeSkills(Interaction interaction, string currentUserId)
    {
        if (interaction == null) return (null, null);

        var isTeacher = interaction.User1Id == currentUserId;
        var learn = isTeacher ? interaction.SkillFromRequester?.Name : interaction.SkillFromTeacher?.Name;
        var teach = isTeacher ? interaction.SkillFromTeacher?.Name : interaction.SkillFromRequester?.Name;
        return (learn, teach);
    }
}
