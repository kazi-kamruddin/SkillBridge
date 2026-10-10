using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SkillBridge.Models;

public class ApplicationUser : IdentityUser
{
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<SkillCategory> SkillCategories { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<SkillStage> SkillStages { get; set; }
    public DbSet<UserInformation> UserInformations { get; set; }
    public DbSet<UserSkill> UserSkills { get; set; }
    public DbSet<Interaction> Interactions { get; set; }
    public DbSet<InteractionSession> InteractionSessions { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SkillRequest> SkillRequests { get; set; }
    public DbSet<UserRating> UserRatings { get; set; }
    public DbSet<Community> Communities { get; set; }
    public DbSet<CommunityPost> CommunityPosts { get; set; }
    public DbSet<CommunityComment> CommunityComments { get; set; }
    public DbSet<CommunityReport> CommunityReports { get; set; }
    public DbSet<MemberBlock> MemberBlocks { get; set; }
    public DbSet<ProfileReport> ProfileReports { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<SavedProfile> SavedProfiles { get; set; }
    public DbSet<InteractionSessionNote> InteractionSessionNotes { get; set; }
    public DbSet<InteractionMeetingEvent> InteractionMeetingEvents { get; set; }
    public DbSet<InteractionPlanProposal> InteractionPlanProposals { get; set; }
    public DbSet<SkillSuggestion> SkillSuggestions { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("skillbridge");
        // The MVC 5 database keys external logins by all three columns.
        modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(login =>
            new { login.LoginProvider, login.ProviderKey, login.UserId });
        modelBuilder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");

        modelBuilder.Entity<Interaction>().HasOne(i => i.User1).WithMany()
            .HasForeignKey(i => i.User1Id).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Interaction>().HasOne(i => i.User2).WithMany()
            .HasForeignKey(i => i.User2Id).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Interaction>().HasOne(i => i.SkillFromTeacher).WithMany()
            .HasForeignKey(i => i.SkillFromTeacherId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Interaction>().HasOne(i => i.SkillFromRequester).WithMany()
            .HasForeignKey(i => i.SkillFromRequesterId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Rating>().HasOne(r => r.FromUser).WithMany()
            .HasForeignKey(r => r.FromUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Rating>().HasOne(r => r.ToUser).WithMany()
            .HasForeignKey(r => r.ToUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SkillRequest>().HasOne(r => r.Requester).WithMany()
            .HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SkillRequest>().HasOne(r => r.Receiver).WithMany()
            .HasForeignKey(r => r.ReceiverId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Conversation>().HasOne(c => c.User1).WithMany()
            .HasForeignKey(c => c.User1Id).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Conversation>().HasOne(c => c.User2).WithMany()
            .HasForeignKey(c => c.User2Id).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Message>().HasOne(m => m.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Message>().HasOne(m => m.FromUser).WithMany()
            .HasForeignKey(m => m.FromUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Message>().HasOne(m => m.ToUser).WithMany()
            .HasForeignKey(m => m.ToUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserRating>().HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SavedProfile>().HasKey(s => new { s.UserId, s.TargetUserId });
        modelBuilder.Entity<SavedProfile>().HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SavedProfile>().HasOne(s => s.TargetUser).WithMany()
            .HasForeignKey(s => s.TargetUserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InteractionSessionNote>().HasIndex(n => new { n.InteractionSessionId, n.UserId }).IsUnique();
        modelBuilder.Entity<InteractionSessionNote>().HasOne(n => n.InteractionSession).WithMany()
            .HasForeignKey(n => n.InteractionSessionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InteractionSessionNote>().HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<InteractionMeetingEvent>().HasOne(e => e.Interaction).WithMany()
            .HasForeignKey(e => e.InteractionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InteractionPlanProposal>().HasKey(p => new { p.InteractionId, p.SkillId });
        modelBuilder.Entity<InteractionPlanProposal>().HasOne(p => p.Interaction).WithMany()
            .HasForeignKey(p => p.InteractionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InteractionPlanProposal>().HasOne(p => p.Skill).WithMany()
            .HasForeignKey(p => p.SkillId).OnDelete(DeleteBehavior.NoAction);
    }
}
