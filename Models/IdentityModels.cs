using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace SkillBridge.Models
{    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }



    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

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
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }



        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Interaction>()
                .HasRequired(i => i.User1)
                .WithMany()
                .HasForeignKey(i => i.User1Id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Interaction>()
                .HasRequired(i => i.User2)
                .WithMany()
                .HasForeignKey(i => i.User2Id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rating>()
                .HasRequired(r => r.FromUser)
                .WithMany()
                .HasForeignKey(r => r.FromUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rating>()
                .HasRequired(r => r.ToUser)
                .WithMany()
                .HasForeignKey(r => r.ToUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Interaction>()
                .HasRequired(i => i.SkillFromTeacher)
                .WithMany()
                .HasForeignKey(i => i.SkillFromTeacherId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Interaction>()
                .HasRequired(i => i.SkillFromRequester)
                .WithMany()
                .HasForeignKey(i => i.SkillFromRequesterId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SkillRequest>()
                .HasRequired(r => r.Requester)
                .WithMany()
                .HasForeignKey(r => r.RequesterId)
                .WillCascadeOnDelete(false);  

            modelBuilder.Entity<SkillRequest>()
                .HasRequired(r => r.Receiver)
                .WithMany()
                .HasForeignKey(r => r.ReceiverId)
                .WillCascadeOnDelete(false); 

            modelBuilder.Entity<SkillRequest>()
                .HasRequired(r => r.Skill)
                .WithMany()
                .HasForeignKey(r => r.SkillId)
                .WillCascadeOnDelete(false);
            modelBuilder.Entity<Conversation>()
                .HasRequired(c => c.User1).WithMany()
                .HasForeignKey(c => c.User1Id).WillCascadeOnDelete(false);

            modelBuilder.Entity<Conversation>()
                .HasRequired(c => c.User2).WithMany()
                .HasForeignKey(c => c.User2Id).WillCascadeOnDelete(false);

            modelBuilder.Entity<Message>()
                .HasRequired(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Message>()
                .HasRequired(m => m.FromUser).WithMany()
                .HasForeignKey(m => m.FromUserId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Message>()
                .HasRequired(m => m.ToUser).WithMany()
                .HasForeignKey(m => m.ToUserId).WillCascadeOnDelete(false);
        }


        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}
