using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using SkillBridge.Controllers;
using SkillBridge.Helpers;
using SkillBridge.Models;

// Finalize the exact EF model used by the app without connecting to Supabase.
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseLazyLoadingProxies()
    .UseNpgsql("Host=localhost;Database=skillbridge_model_check;Username=unused;Password=unused")
    .Options;
using var db = new ApplicationDbContext(options);
_ = db.Model.GetEntityTypes().Count();
var interactionEntity = db.Model.FindEntityType(typeof(Interaction))
    ?? throw new Exception("Interaction entity missing from EF model.");
var requesterSkillProp = interactionEntity.FindProperty(nameof(Interaction.SkillFromRequesterId))
    ?? throw new Exception("SkillFromRequesterId property missing from EF model.");
if (!requesterSkillProp.IsNullable)
    throw new Exception("Interaction.SkillFromRequesterId must be nullable to support 1-way mentorships.");
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

// D4: Strict separation between catalog-linked progression and custom milestones.
var customOnlySessions = new[]
{
    new InteractionSession { SkillId = 1, StageNumber = 1, CatalogStageNumber = null, User1Confirmed = true, User2Confirmed = true },
    new InteractionSession { SkillId = 1, StageNumber = 2, CatalogStageNumber = null, User1Confirmed = true, User2Confirmed = true }
};
if (ExchangeProgressHelper.MaxStageCompleted(customOnlySessions, 1) != 0)
    throw new Exception("Custom milestones without CatalogStageNumber must return stage 0.");

var contiguousCatalogSessions = new[]
{
    new InteractionSession { SkillId = 1, StageNumber = 1, CatalogStageNumber = 1, User1Confirmed = true, User2Confirmed = true },
    new InteractionSession { SkillId = 1, StageNumber = 2, CatalogStageNumber = 2, User1Confirmed = true, User2Confirmed = true },
    new InteractionSession { SkillId = 1, StageNumber = 3, CatalogStageNumber = 4, User1Confirmed = true, User2Confirmed = true }
};
if (ExchangeProgressHelper.MaxStageCompleted(contiguousCatalogSessions, 1) != 2)
    throw new Exception("Contiguous catalog completion check failed (expected 2 when stage 3 is skipped).");

var unconfirmedCatalogSessions = new[]
{
    new InteractionSession { SkillId = 1, StageNumber = 1, CatalogStageNumber = 1, User1Confirmed = true, User2Confirmed = false }
};
if (ExchangeProgressHelper.MaxStageCompleted(unconfirmedCatalogSessions, 1) != 0)
    throw new Exception("Single-sided confirmation must not advance verified catalog stage.");
Console.WriteLine("Catalog vs custom stage progression checks passed.");

// D3: Null-safe 1-way mentorship vs 2-way reciprocal skill formatting.
var oneWayInteraction = new Interaction
{
    User1Id = "teacher-1",
    User2Id = "learner-2",
    SkillFromTeacherId = 1,
    SkillFromTeacher = sampleSkills[0],
    SkillFromRequesterId = null,
    SkillFromRequester = null
};
var (teacherLearns, teacherTeaches) = ExchangeProgressHelper.FormatExchangeSkills(oneWayInteraction, "teacher-1");
var (learnerLearns, learnerTeaches) = ExchangeProgressHelper.FormatExchangeSkills(oneWayInteraction, "learner-2");
if (teacherLearns != null || teacherTeaches != "Python" || learnerLearns != "Python" || learnerTeaches != null)
    throw new Exception("1-way mentorship skill formatting mismatch.");

var twoWayInteraction = new Interaction
{
    User1Id = "teacher-1",
    User2Id = "learner-2",
    SkillFromTeacherId = 1,
    SkillFromTeacher = sampleSkills[0],
    SkillFromRequesterId = 2,
    SkillFromRequester = sampleSkills[1]
};
var (t2Learns, t2Teaches) = ExchangeProgressHelper.FormatExchangeSkills(twoWayInteraction, "teacher-1");
if (t2Learns != "Figma" || t2Teaches != "Python")
    throw new Exception("2-way exchange skill formatting mismatch.");
Console.WriteLine("One-way & two-way skill formatting checks passed.");

// D5: Profile view model explicit opt-in metadata checks.
var profileSkillVm = new UserSkillViewModel { SkillId = 1, SkillName = "Python", KnownUpToStage = 2, AlreadyTeaching = false };
var updateVm = new UpdateProfileViewModel { VerifiedLearningStages = new Dictionary<int, int> { [1] = 2 } };
if (profileSkillVm.SkillId != 1 || profileSkillVm.AlreadyTeaching || updateVm.VerifiedLearningStages[1] != 2)
    throw new Exception("Profile verified learning & opt-in model checks failed.");
Console.WriteLine("Profile verified learning model checks passed.");

// D7/D8/D9/D10: Phase 3 community attribution, inbox models, notifications, SignalR badge helper, & ctor compatibility.
var stubUrl = new StubUrlHelper();
var communityNotif = new Notification { Type = "Community", ReferenceId = 42 };
var resolvedCommunityUrl = NotificationLinkHelper.Resolve(communityNotif, stubUrl);
if (resolvedCommunityUrl != "/Communities/PostDetails?id=42")
    throw new Exception($"NotificationLinkHelper must resolve Community notifications cleanly (got '{resolvedCommunityUrl}').");

var commentVm = new CommunityCommentViewModel
{
    CommentId = 9,
    CreatedByUserId = "author-1",
    CreatedByFullName = "Demo Author",
    AuthorIsPublic = true
};
var partnerVm = new ChatPartnerViewModel { UserId = "partner-2", FullName = "Demo Partner" };
var inboxVm = new InboxConversationViewModel
{
    ConversationId = 5,
    PartnerUserId = "partner-2",
    PartnerName = "Demo Partner",
    UnreadCount = 2
};
if (commentVm.CreatedByUserId != "author-1" || !commentVm.AuthorIsPublic ||
    partnerVm.UserId != "partner-2" || inboxVm.ConversationId != 5 || inboxVm.UnreadCount != 2)
    throw new Exception("Phase 3 ViewModel property checks failed.");

await HubNotifications.NotifyBadgesAsync(null, null, "user-1", "", null);

using var interactionsCtrl = new InteractionsController(db);
using var catalogCtrl = new CatalogController(db);
using var notificationsCtrl = new NotificationsController(db);
Console.WriteLine("Phase 3 notification link, inbox model, SignalR null-safety, and controller ctor checks passed.");

sealed class StubUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; } = new();

    public string Action(UrlActionContext actionContext)
    {
        var values = new RouteValueDictionary(actionContext.Values);
        var id = values.TryGetValue("id", out var v) ? $"?id={v}" : "";
        return $"/{actionContext.Controller}/{actionContext.Action}{id}";
    }

    public string Content(string contentPath) => contentPath;
    public bool IsLocalUrl(string url) => true;
    public string Link(string routeName, object values) => routeName;
    public string RouteUrl(UrlRouteContext routeContext) => "/";
}
