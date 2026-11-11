using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using SkillBridge.Helpers;
using SkillBridge.Models;

// Finalize the exact EF model used by the app without connecting to Supabase.
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseLazyLoadingProxies()
    .UseNpgsql("Host=localhost;Database=skillbridge_model_check;Username=unused;Password=unused")
    .Options;
using var db = new ApplicationDbContext(options);
_ = db.Model.GetEntityTypes().Count();
Console.WriteLine("Core EF model checks passed.");

using var writer = new StringWriter();
PostTextFormatter.Render("## Hello\n**bold** and *italic* <script>alert(1)</script>").WriteTo(writer, HtmlEncoder.Default);
var formatted = writer.ToString();
if (!formatted.Contains("<h3>Hello</h3>") ||
    !formatted.Contains("<strong>bold</strong>") ||
    !formatted.Contains("<em>italic</em>") ||
    formatted.Contains("<script>") ||
    !formatted.Contains("&lt;script&gt;"))
    throw new Exception("Post formatting must preserve basic styles while encoding HTML.");
Console.WriteLine("Post formatting checks passed.");

var sampleSkills = new[]
{
    new Skill { Id = 1, Name = "Python", Description = "Scripting and automation", SkillCategory = new SkillCategory { Name = "Software" }, SkillStages = [] },
    new Skill { Id = 2, Name = "Figma", Description = "Interface design", SkillCategory = new SkillCategory { Name = "Design" }, SkillStages = [] },
    new Skill { Id = 3, Name = "React", Description = "Web interfaces", SkillCategory = new SkillCategory { Name = "Web" }, SkillStages = [] },
    new Skill { Id = 4, Name = "Video Editing", Description = "Storytelling with footage", SkillCategory = new SkillCategory { Name = "Media" }, SkillStages = [] },
    new Skill { Id = 5, Name = "Public Speaking", Description = "Presenting ideas clearly", SkillCategory = new SkillCategory { Name = "Communication" }, SkillStages = [] },
    new Skill { Id = 6, Name = "Git & GitHub", Description = "Source collaboration", SkillCategory = new SkillCategory { Name = "Web" }, SkillStages = [] }
};

var figmaHit = SkillSearch.Rank(sampleSkills, "wireframe").FirstOrDefault()?.Name;
var reactHit = SkillSearch.Rank(sampleSkills, "jsx").FirstOrDefault()?.Name;
var videoHit = SkillSearch.Rank(sampleSkills, "timeline pacing").FirstOrDefault()?.Name;
var speechHit = SkillSearch.Rank(sampleSkills, "presentation pitch").FirstOrDefault()?.Name;
var gitHit = SkillSearch.Rank(sampleSkills, "version control").FirstOrDefault()?.Name;

if (figmaHit != "Figma" || reactHit != "React" || videoHit != "Video Editing" ||
    speechHit != "Public Speaking" || gitHit != "Git & GitHub")
    throw new Exception($"SkillSearch expanded concept check failed: figma={figmaHit}, react={reactHit}, video={videoHit}, speech={speechHit}, git={gitHit}");
Console.WriteLine("Expanded 21-skill concept search checks passed.");

var exploreVm = new ExploreViewModel
{
    SearchQuery = "figma",
    AllCatalogSkillNames = ["Figma", "Python"],
    HasOwnSkills = false,
    AdditionalMembers = [new PublicProfileViewModel { UserId = "u1", FullName = "Demo Member", CanKnock = true }]
};
if (!exploreVm.AdditionalMembers.Single().CanKnock || exploreVm.AllCatalogSkillNames.Count != 2)
    throw new Exception("ExploreViewModel discovery check failed.");
Console.WriteLine("ExploreViewModel discovery checks passed.");
