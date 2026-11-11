using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillBridge.Controllers;
using SkillBridge.Models;

// Opt-in checks use only the two supplied disposable accounts. Every fixture is removed in finally.
if (Environment.GetEnvironmentVariable("SKILLBRIDGE_MUTATING_TESTS") != "1" ||
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKILLBRIDGE_TEST_ACCOUNTS_FILE")))
    throw new InvalidOperationException("Enable disposable-account writes and provide SKILLBRIDGE_TEST_ACCOUNTS_FILE.");
using var accounts = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("SKILLBRIDGE_TEST_ACCOUNTS_FILE")));
var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SKILLBRIDGE_DB_CONNECTION"))
{
    SslMode = SslMode.VerifyFull,
    RootCertificate = Environment.GetEnvironmentVariable("SKILLBRIDGE_DB_CA_CERT")
        ?? Path.GetFullPath("SkillBridge.Core/certs/prod-ca-2021.crt")
};
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseLazyLoadingProxies()
    .UseNpgsql(connection.ConnectionString).Options;
using var db = new ApplicationDbContext(options);
var ids = new[] { "accountA", "accountB" }.Select(key =>
{
    var email = accounts.RootElement.GetProperty(key).GetProperty("email").GetString();
    return db.Users.Where(user => user.Email == email).Select(user => user.Id).Single();
}).ToArray();
if (ids[0] == ids[1]) throw new InvalidOperationException("Provide two different disposable accounts.");
var statistics = db.UserRatings.AsNoTracking().Where(r => ids.Contains(r.UserId)).ToList();
if (statistics.Count != 2) throw new InvalidOperationException("Both test accounts need existing rating statistics.");
var originalSkills = db.UserSkills.AsNoTracking().Where(s => ids.Contains(s.UserId)).ToList();
var skillIds = db.Skills.OrderBy(s => s.Id).Take(2).Select(s => s.Id).ToArray();
if (skillIds.Length != 2) throw new InvalidOperationException("The catalog needs two skills.");

var learnerTouchedSkillIds = originalSkills.Where(s => s.UserId == ids[1]).Select(s => s.SkillId).ToHashSet();
var mentorshipSkill = db.Skills
    .Include(s => s.SkillStages)
    .AsNoTracking()
    .Where(s => s.SkillStages.Count >= 2)
    .OrderBy(s => learnerTouchedSkillIds.Contains(s.Id) ? 1 : 0)
    .ThenBy(s => s.Id)
    .FirstOrDefault() ?? throw new InvalidOperationException("The catalog needs a skill with at least two stages.");
var originalLearnerSkillRows = originalSkills
    .Where(s => s.UserId == ids[1] && s.SkillId == mentorshipSkill.Id)
    .ToList();

var fixture = new Interaction
{
    User1Id = ids[0], User2Id = ids[1], SkillFromTeacherId = skillIds[0],
    SkillFromRequesterId = skillIds[1], Status = "Ongoing"
};
db.Interactions.Add(fixture);
db.SaveChanges();
Interaction oneWayFixture = null;
try
{
    foreach (var skillId in skillIds)
        db.InteractionSessions.Add(new InteractionSession
        {
            InteractionId = fixture.Id, SkillId = skillId, StageNumber = 1,
            Title = "Temporary audit custom milestone", CatalogStageNumber = null,
            Status = "Pending", User1Confirmed = false, User2Confirmed = false
        });
    db.SaveChanges();

    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[0]).ProposeMeeting(fixture.Id, DateTimeOffset.UtcNow.AddHours(2).ToString("o"),
            "Online", "Temporary audit meeting") is RedirectToActionResult, "Meeting proposal must succeed.");
    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[1]).RespondToMeeting(fixture.Id, "Accept") is RedirectToActionResult &&
            context.Interactions.Find(fixture.Id).MeetingStatus == "Confirmed", "The partner must be able to accept the meeting.");
    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[0]).CancelMeeting(fixture.Id) is RedirectToActionResult &&
            context.InteractionMeetingEvents.Count(e => e.InteractionId == fixture.Id) == 3,
            "Meeting changes must preserve their history.");

    var sessions = db.InteractionSessions.AsNoTracking().Where(s => s.InteractionId == fixture.Id).OrderBy(s => s.Id).ToList();
    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[0]).SaveStageNote(sessions[0].Id, "Temporary audit note", "Next step") is RedirectToActionResult,
            "A participant must be able to save a milestone note.");
    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[0]).ProposePlan(fixture.Id, sessions[0].SkillId,
            new List<PlanStepInput> { new() { SessionId = sessions[0].Id, Title = "Temporary revised custom milestone" } }) is RedirectToActionResult,
            "An upcoming plan must be editable without deleting notes.");
    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[1]).RespondToPlan(fixture.Id, sessions[0].SkillId, "Accept") is RedirectToActionResult &&
            context.InteractionSessions.Find(sessions[0].Id).Title == "Temporary revised custom milestone",
            "The partner must be able to accept the revised plan.");
    foreach (var session in sessions)
        foreach (var userId in ids)
        {
            using var context = new ApplicationDbContext(options);
            Check(await Controller(context, userId).MarkStageDone(session.StageNumber, fixture.Id, session.SkillId) is JsonResult,
                "Both participants must be able to confirm each milestone.");
        }
    Check(db.InteractionSessions.AsNoTracking().Where(s => s.InteractionId == fixture.Id)
        .All(s => s.User1Confirmed && s.User2Confirmed && s.Status == "Confirmed"), "Both confirmations must persist.");
    Console.WriteLine("Meeting, plan, note and milestone workflow checks passed.");

    using var start = new ManualResetEventSlim(false);
    var completions = ids.Select(userId => Task.Run(async () =>
    {
        using var context = new ApplicationDbContext(options);
        start.Wait();
        return await Controller(context, userId).EndInteraction(fixture.Id);
    })).ToArray();
    start.Set();
    var completed = await Task.WhenAll(completions);
    Check(completed.Count(result => result is RedirectToActionResult) == 1 &&
        completed.Count(result => result is StatusCodeResult { StatusCode: 409 }) == 1,
        "Exactly one simultaneous completion must succeed.");
    Check(db.Notifications.Count(n => n.Type == "Feedback" && n.ReferenceId == fixture.Id) == 2,
        "Completion must create one feedback notification per partner.");
    foreach (var before in statistics)
        Check(db.UserRatings.AsNoTracking().Single(r => r.UserId == before.UserId).InteractionsCompleted == before.InteractionsCompleted + 1,
            "Each completion count must increase once.");
    var currentSkills = db.UserSkills.AsNoTracking().Where(s => ids.Contains(s.UserId)).OrderBy(s => s.Id).ToList();
    Check(currentSkills.Select(s => (s.Id, s.KnownUpToStage)).SequenceEqual(
        originalSkills.OrderBy(s => s.Id).Select(s => (s.Id, s.KnownUpToStage))),
        "Custom-only milestones must not add a teaching skill or raise catalog progress.");
    Console.WriteLine("Completion race and custom milestone checks passed.");

    using var ratingStart = new ManualResetEventSlim(false);
    var ratings = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
    {
        using var context = new ApplicationDbContext(options);
        ratingStart.Wait();
        var result = (JsonResult)await Controller(context, ids[0]).SubmitRating(new InteractionRatingViewModel
        {
            InteractionId = fixture.Id, RatingValue = 9, Comment = "Temporary audit concurrency check"
        });
        return JsonSerializer.SerializeToElement(result.Value).GetProperty("success").GetBoolean();
    })).ToArray();
    ratingStart.Set();
    Check((await Task.WhenAll(ratings)).Count(success => success) == 1, "Only one simultaneous rating may succeed.");
    var recipient = db.UserRatings.AsNoTracking().Single(r => r.UserId == ids[1]);
    var originalRecipient = statistics.Single(r => r.UserId == ids[1]);
    Check(recipient.RatingsReceived == originalRecipient.RatingsReceived + 1 &&
        recipient.AccumulatedRating == originalRecipient.AccumulatedRating + 9,
        "A duplicate rating must not change the recipient's totals twice.");
    Console.WriteLine("Duplicate rating checks passed.");

    // Phase 2: One-way mentorship (SkillFromRequesterId = null) + verified catalog progression without silent Teaching promotion.
    oneWayFixture = new Interaction
    {
        User1Id = ids[0],
        User2Id = ids[1],
        SkillFromTeacherId = mentorshipSkill.Id,
        SkillFromRequesterId = null,
        Status = "Ongoing"
    };
    db.Interactions.Add(oneWayFixture);
    db.SaveChanges();

    var catalogStages = mentorshipSkill.SkillStages.OrderBy(s => s.StageNumber).Take(2).ToList();
    foreach (var stage in catalogStages)
    {
        db.InteractionSessions.Add(new InteractionSession
        {
            InteractionId = oneWayFixture.Id,
            SkillId = mentorshipSkill.Id,
            StageNumber = stage.StageNumber,
            Title = stage.Description,
            CatalogStageNumber = stage.StageNumber,
            Status = "Pending",
            User1Confirmed = false,
            User2Confirmed = false
        });
    }
    db.SaveChanges();

    using (var context = new ApplicationDbContext(options))
    {
        Check(await Controller(context, ids[0]).Index() is ViewResult, "Teacher Interactions/Index must handle 1-way mentorships.");
        Check(await Controller(context, ids[1]).Sessions(oneWayFixture.Id) is ViewResult, "Learner Sessions page must load for 1-way mentorships.");
    }

    var oneWaySessions = db.InteractionSessions.AsNoTracking().Where(s => s.InteractionId == oneWayFixture.Id).OrderBy(s => s.StageNumber).ToList();
    foreach (var session in oneWaySessions)
        foreach (var userId in ids)
        {
            using var context = new ApplicationDbContext(options);
            Check(await Controller(context, userId).MarkStageDone(session.StageNumber, oneWayFixture.Id, session.SkillId) is JsonResult,
                "1-way mentorship catalog milestones must confirm cleanly.");
        }

    var priorTeaching = originalLearnerSkillRows.FirstOrDefault(s => s.Status == "Teaching");
    using (var context = new ApplicationDbContext(options))
        Check(await Controller(context, ids[0]).EndInteraction(oneWayFixture.Id) is RedirectToActionResult,
            "1-way mentorship must complete cleanly without a reciprocal skill.");

    Check(db.Notifications.Count(n => n.Type == "Feedback" && n.ReferenceId == oneWayFixture.Id) == 2,
        "1-way mentorship completion must create feedback notifications cleanly.");

    var updatedLearning = db.UserSkills.AsNoTracking()
        .FirstOrDefault(s => s.UserId == ids[1] && s.SkillId == mentorshipSkill.Id && s.Status == "Learning");
    Check(updatedLearning != null && (updatedLearning.KnownUpToStage ?? 0) >= 2,
        "Completing catalog stages in a 1-way mentorship must raise verified Learning stage progress.");

    var postTeaching = db.UserSkills.AsNoTracking()
        .FirstOrDefault(s => s.UserId == ids[1] && s.SkillId == mentorshipSkill.Id && s.Status == "Teaching");
    if (priorTeaching == null)
        Check(postTeaching == null, "Completing catalog stages must never silently add a Teaching row.");
    else
        Check(postTeaching != null && postTeaching.KnownUpToStage == priorTeaching.KnownUpToStage,
            "Completing catalog stages must not silently mutate existing Teaching stage levels.");

    Console.WriteLine("One-way mentorship, catalog progression, and no-auto-teach checks passed.");
}
finally
{
    using var cleanup = new ApplicationDbContext(options);
    using var transaction = cleanup.Database.BeginTransaction();
    var fixtureIds = oneWayFixture != null ? new[] { fixture.Id, oneWayFixture.Id } : new[] { fixture.Id };
    cleanup.Notifications.Where(n => n.ReferenceId.HasValue && fixtureIds.Contains(n.ReferenceId.Value) &&
        (n.Type == "Feedback" || n.Type == "Exchange")).ExecuteDelete();
    cleanup.Ratings.Where(r => fixtureIds.Contains(r.InteractionId)).ExecuteDelete();
    cleanup.InteractionSessionNotes.Where(n => fixtureIds.Contains(n.InteractionSession.InteractionId)).ExecuteDelete();
    cleanup.InteractionPlanProposals.Where(p => fixtureIds.Contains(p.InteractionId)).ExecuteDelete();
    cleanup.InteractionMeetingEvents.Where(e => fixtureIds.Contains(e.InteractionId)).ExecuteDelete();
    cleanup.InteractionSessions.Where(s => fixtureIds.Contains(s.InteractionId)).ExecuteDelete();
    cleanup.Interactions.Where(i => fixtureIds.Contains(i.Id)).ExecuteDelete();
    foreach (var before in statistics)
        cleanup.UserRatings.Where(r => r.UserId == before.UserId).ExecuteUpdate(setters => setters
            .SetProperty(r => r.InteractionsCompleted, before.InteractionsCompleted)
            .SetProperty(r => r.RatingsReceived, before.RatingsReceived)
            .SetProperty(r => r.AccumulatedRating, before.AccumulatedRating));

    var originalIds = originalSkills.Select(s => s.Id).ToArray();
    cleanup.UserSkills.Where(s => ids.Contains(s.UserId) && !originalIds.Contains(s.Id)).ExecuteDelete();
    foreach (var beforeSkill in originalLearnerSkillRows)
        cleanup.UserSkills.Where(s => s.Id == beforeSkill.Id)
            .ExecuteUpdate(setters => setters.SetProperty(s => s.KnownUpToStage, beforeSkill.KnownUpToStage));

    transaction.Commit();
    Console.WriteLine("Temporary workflow fixtures removed; disposable-account statistics and skills restored.");
}

static InteractionsController Controller(ApplicationDbContext db, string userId)
{
    var http = new DefaultHttpContext
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Audit"))
    };
    return new InteractionsController(db)
    {
        ControllerContext = new ControllerContext { HttpContext = http },
        TempData = new TempDataDictionary(http, new AuditTempDataProvider())
    };
}
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class AuditTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
