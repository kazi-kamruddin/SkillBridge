using System.Text.RegularExpressions;
using SkillBridge.Models;

namespace SkillBridge.Helpers;

// Intent terms for the curated catalog; no external model or API is required.
public static class SkillSearch
{
    private static readonly Dictionary<string, string[]> Concepts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Python"] = ["automation", "scripting", "programming", "code", "software"],
        ["C++"] = ["programming", "code", "software", "systems", "performance", "pointers"],
        ["Java"] = ["programming", "code", "software", "object oriented", "backend"],
        ["AutoCAD"] = ["cad", "2d design", "technical drawing", "drafting", "blueprint"],
        ["MATLAB"] = ["matrices", "simulation", "numerical computing", "engineering analysis", "plotting", "visualization"],
        ["SolidWorks"] = ["cad", "3d design", "mechanical design", "parts", "assemblies", "modeling"],
        ["Statistics"] = ["probability", "data analysis", "hypothesis", "research", "regression"],
        ["Excel"] = ["spreadsheet", "spreadsheets", "tables", "formulas", "pivot tables", "charts", "data analysis"],
        ["SQL"] = ["database", "databases", "queries", "query", "data storage", "relational data", "postgres"]
    };

    public static IEnumerable<Skill> Rank(IEnumerable<Skill> skills, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return skills.OrderBy(s => s.Name);
        var words = Tokens(query).ToArray();
        if (words.Length == 0) return skills.OrderBy(s => s.Name);
        return skills.Select(skill => new { skill, score = Score(skill, query, words) })
            .Where(item => item.score > 0)
            .OrderByDescending(item => item.score).ThenBy(item => item.skill.Name)
            .Select(item => item.skill);
    }

    private static int Score(Skill skill, string query, string[] words)
    {
        var name = skill.Name.ToLowerInvariant();
        var text = string.Join(" ", new[] { skill.Description ?? "", skill.SkillCategory?.Name ?? "" }
            .Concat(skill.SkillStages?.Select(stage => stage.Description) ?? []));
        var contentWords = Tokens(text).ToHashSet();
        var score = name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ? 12 : 0;
        score += words.Count(word => Tokens(name).Contains(word)) * 6;
        score += words.Count(contentWords.Contains) * 2;
        if (Concepts.TryGetValue(skill.Name, out var concepts))
            foreach (var concept in concepts)
                if (Tokens(concept).All(words.Contains)) score += 8;
        return score;
    }

    private static IEnumerable<string> Tokens(string text) =>
        Regex.Matches(text.ToLowerInvariant(), "[a-z0-9+#]+")
            .Select(match => match.Value)
            .Where(word => word.Length > 1 && word is not "to" and not "the" and not "and" and not "for" and not "how" and not "with" and not "learn" and not "want" and not "make" and not "build");
}
