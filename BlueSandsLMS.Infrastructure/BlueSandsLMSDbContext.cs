using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Core.Common;


namespace BlueSandsLMS.Infrastructure
{

    public partial class BlueSandsLMSDbContext : DbContext
    {
        public BlueSandsLMSDbContext(DbContextOptions<BlueSandsLMSDbContext> options)
            : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<School> Schools => Set<School>();
        public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
        public DbSet<Classroom> Classrooms => Set<Classroom>();
        public DbSet<ParentLink> ParentLinks => Set<ParentLink>();

        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<ExperimentLaunch> ExperimentLaunches => Set<ExperimentLaunch>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<ClassroomTeacher> ClassroomTeachers { get; set; } = null!;
public DbSet<ClassMessage> ClassMessages { get; set; } = null!;
public DbSet<ClassMessageRead> ClassMessageReads { get; set; } = null!;

public DbSet<TicketComment> TicketComments => Set<TicketComment>();
public DbSet<PhETSimulation> PhETSimulations => Set<PhETSimulation>();
public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<InteractiveLearningSpace> InteractiveLearningSpaces => Set<InteractiveLearningSpace>();
        public DbSet<CurriculumTag> CurriculumTags => Set<CurriculumTag>();
        public DbSet<IlsTag> IlsTags => Set<IlsTag>();
        public DbSet<StudentIlsSession> StudentIlsSessions => Set<StudentIlsSession>();
        public DbSet<SessionPoll> SessionPolls => Set<SessionPoll>();
        public DbSet<SessionHypothesis> SessionHypotheses => Set<SessionHypothesis>();
        public DbSet<SessionTrial> SessionTrials => Set<SessionTrial>();
        public DbSet<SessionReflection> SessionReflections => Set<SessionReflection>();
        public DbSet<SessionAssessment> SessionAssessments => Set<SessionAssessment>();
        public DbSet<SessionOrientation> SessionOrientations => Set<SessionOrientation>();
        public DbSet<SessionRealWorld> SessionRealWorlds => Set<SessionRealWorld>();
        public DbSet<IlsDiscussionMessage> IlsDiscussionMessages => Set<IlsDiscussionMessage>();

        public DbSet<MessageLog> MessageLogs { get; set; }
        public DbSet<ForumTopic> ForumTopics { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }

        public DbSet<BadgeAward> BadgeAwards => Set<BadgeAward>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
public DbSet<SchoolInquiry> SchoolInquiries { get; set; }
public DbSet<IndividualInquiry> IndividualInquiries { get; set; }
        public DbSet<PricingTier> PricingTiers => Set<PricingTier>();
        public DbSet<PricingPromo> PricingPromos => Set<PricingPromo>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<SupportResource> SupportResources => Set<SupportResource>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();

         partial void OnModelCreatingStudentContent(ModelBuilder modelBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            OnModelCreatingStudentContent(modelBuilder);


            modelBuilder.Entity<Classroom>().HasIndex(x => new { x.SchoolId, x.Name });

            modelBuilder.Entity<Enrollment>()
                .HasIndex(x => new { x.ClassroomId, x.UserId }).IsUnique(false);

            modelBuilder.Entity<Assignment>()
                .HasIndex(x => new { x.ClassroomId, x.DueAt });

            modelBuilder.Entity<Submission>()
                .HasIndex(x => new { x.AssignmentId, x.StudentId, x.Status });

            modelBuilder.Entity<ExperimentLaunch>().HasIndex(x => new { x.UserId, x.StartedAt });
            modelBuilder.Entity<ExperimentLaunch>().HasIndex(x => new { x.ClassroomId, x.StartedAt });
            modelBuilder.Entity<ExperimentLaunch>().HasIndex(x => new { x.UserId, x.ExperimentName, x.StartedAt });

            modelBuilder.Entity<QuizAttempt>()
               .HasIndex(x => new { x.UserId, x.QuizCode, x.CompletedAt });

            modelBuilder.Entity<BadgeAward>().HasIndex(x => new { x.UserId, x.AwardedAt });
            modelBuilder.Entity<BadgeAward>().HasIndex(x => new { x.UserId, x.Code }).IsUnique();

            modelBuilder.Entity<Announcement>()
                .HasIndex(x => new { x.SchoolId, x.ClassroomId, x.CreatedAt });

            modelBuilder.Entity<AuditEvent>()
                .HasIndex(x => new { x.SchoolId, x.Utc, x.Category, x.Name });

            modelBuilder.Entity<InteractiveLearningSpace>(entity =>
            {
                entity.HasIndex(x => new { x.CreatedBy, x.Status });
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => new { x.SchoolId, x.IsSharedWithSchool });
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Objective).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.Grade).HasMaxLength(50).IsRequired();
                entity.Property(x => x.PollOptionsJson).HasColumnType("nvarchar(max)");
                entity.Property(x => x.AssessmentConfigJson).HasColumnType("nvarchar(max)");
                entity.Property(x => x.IsSharedWithSchool).HasDefaultValue(false);

                entity.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.SimulationSource).HasMaxLength(20).HasDefaultValue("phet").IsRequired();
                entity.Property(x => x.PraxiLabsExperimentId).HasMaxLength(100).IsRequired(false);

                entity.HasOne(x => x.Simulation)
                    .WithMany()
                    .HasForeignKey(x => x.SimulationId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasOne(x => x.School)
                    .WithMany()
                    .HasForeignKey(x => x.SchoolId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            modelBuilder.Entity<CurriculumTag>(entity =>
            {
                entity.Property(x => x.Label).HasMaxLength(120).IsRequired();
                entity.Property(x => x.Subject).HasMaxLength(80).IsRequired();
                entity.HasIndex(x => new { x.Label, x.Subject }).IsUnique();
            });

            modelBuilder.Entity<IlsTag>(entity =>
            {
                entity.HasKey(x => new { x.IlsId, x.TagId });
                entity.HasOne(x => x.Ils)
                    .WithMany(x => x.Tags)
                    .HasForeignKey(x => x.IlsId);
                entity.HasOne(x => x.Tag)
                    .WithMany(x => x.Ils)
                    .HasForeignKey(x => x.TagId);
            });

            modelBuilder.Entity<StudentIlsSession>(entity =>
            {
                entity.HasIndex(x => new { x.StudentId, x.IlsId }).IsUnique();
                entity.HasIndex(x => x.StudentId);
                entity.HasIndex(x => x.IlsId);
                entity.HasIndex(x => new { x.IlsId, x.CurrentStep });
                entity.Property(x => x.CurrentStep).HasDefaultValue(1);
                entity.Property(x => x.ReflectionSubmitted).HasDefaultValue(false);
                entity.Property(x => x.BadgeAwarded).HasDefaultValue(false);

                entity.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Ils)
                    .WithMany(x => x.Sessions)
                    .HasForeignKey(x => x.IlsId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SessionPoll>(entity =>
            {
                entity.HasIndex(x => x.SessionId).IsUnique();
                entity.HasOne(x => x.Session)
                    .WithOne(x => x.Poll)
                    .HasForeignKey<SessionPoll>(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionHypothesis>(entity =>
            {
                entity.HasIndex(x => x.SessionId).IsUnique();
                entity.Property(x => x.Text).HasMaxLength(8000).IsRequired();
                entity.Property(x => x.InputMethod).HasMaxLength(20).IsRequired();
                entity.HasOne(x => x.Session)
                    .WithOne(x => x.Hypothesis)
                    .HasForeignKey<SessionHypothesis>(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionTrial>(entity =>
            {
                entity.HasIndex(x => new { x.SessionId, x.CreatedAt });
                entity.Property(x => x.VariablesJson).IsRequired();
                entity.Property(x => x.ResultJson).IsRequired();
                entity.HasOne(x => x.Session)
                    .WithMany(x => x.Trials)
                    .HasForeignKey(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionReflection>(entity =>
            {
                entity.HasIndex(x => x.SessionId).IsUnique();
                entity.Property(x => x.Text).HasMaxLength(8000).IsRequired();
                entity.HasOne(x => x.Session)
                    .WithOne(x => x.Reflection)
                    .HasForeignKey<SessionReflection>(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionAssessment>(entity =>
            {
                entity.HasIndex(x => x.SessionId).IsUnique();
                entity.Property(x => x.AnswersJson).IsRequired();
                entity.Property(x => x.Feedback).HasMaxLength(4000).IsRequired();
                entity.Property(x => x.Score).HasPrecision(5, 4);
                entity.Property(x => x.IsAutoSubmitted).HasDefaultValue(false);
                entity.HasOne(x => x.Session)
                    .WithOne(x => x.Assessment)
                    .HasForeignKey<SessionAssessment>(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionOrientation>(entity =>
            {
                entity.HasIndex(x => x.SessionId).IsUnique();
                entity.Property(x => x.EngagementAnswer).HasMaxLength(8000).IsRequired();
                entity.HasOne(x => x.Session)
                    .WithOne(x => x.Orientation)
                    .HasForeignKey<SessionOrientation>(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionRealWorld>(entity =>
            {
                entity.HasIndex(x => x.SessionId).IsUnique();
                entity.Property(x => x.Note).HasMaxLength(8000).IsRequired();
                entity.HasOne(x => x.Session)
                    .WithOne(x => x.RealWorld)
                    .HasForeignKey<SessionRealWorld>(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<IlsDiscussionMessage>(entity =>
            {
                entity.HasIndex(x => new { x.IlsId, x.CreatedAt });
                entity.HasIndex(x => new { x.SessionId, x.CreatedAt });
                entity.Property(x => x.Text).HasMaxLength(8000).IsRequired();
                entity.Property(x => x.FlagReason).HasMaxLength(500);

                entity.HasOne(x => x.Ils)
                    .WithMany(x => x.DiscussionMessages)
                    .HasForeignKey(x => x.IlsId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Session)
                    .WithMany(x => x.DiscussionMessages)
                    .HasForeignKey(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Author)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


                modelBuilder.Entity<ClassroomTeacher>()
    .HasKey(x => new { x.ClassroomId, x.TeacherUserId });

modelBuilder.Entity<ClassroomTeacher>()
    .HasOne(x => x.Classroom).WithMany(c => c.Teachers)
    .HasForeignKey(x => x.ClassroomId);

modelBuilder.Entity<ClassroomTeacher>()
    .HasOne(x => x.Teacher).WithMany()
    .HasForeignKey(x => x.TeacherUserId);

modelBuilder.Entity<ClassMessage>()
    .HasOne(m => m.Classroom).WithMany(c => c.Messages)
    .HasForeignKey(m => m.ClassroomId);

modelBuilder.Entity<ClassMessage>()
    .HasOne(m => m.FromUser).WithMany()
    .HasForeignKey(m => m.FromUserId);

modelBuilder.Entity<ClassMessageRead>()
    .HasKey(r => new { r.MessageId, r.UserId });

modelBuilder.Entity<ClassMessageRead>()
    .HasOne(r => r.Message).WithMany(m => m.Reads)
    .HasForeignKey(r => r.MessageId);

modelBuilder.Entity<ClassMessageRead>()
    .HasOne(r => r.User).WithMany()
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.NoAction);


modelBuilder.Entity<SchoolInquiry>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email);
        entity.HasIndex(e => e.DateCreated);
        entity.HasIndex(e => e.IsContacted);
        entity.Property(e => e.DateCreated).HasDefaultValueSql("GETDATE()");
    });


    modelBuilder.Entity<IndividualInquiry>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email);
        entity.HasIndex(e => e.DateCreated);
        entity.HasIndex(e => e.IsContacted);
        entity.Property(e => e.DateCreated).HasDefaultValueSql("GETDATE()");
    });


modelBuilder.Entity<PhETSimulation>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.Title);
    entity.HasIndex(e => e.Physics);
    entity.HasIndex(e => e.Chemistry);
    entity.HasIndex(e => e.MathStatistics);
    entity.HasIndex(e => e.Biology);
    entity.HasIndex(e => e.EarthSpace);
    entity.HasIndex(e => e.LowGradeLevel);
    entity.HasIndex(e => e.HighGradeLevel);
    entity.HasIndex(e => e.IsActive);
    entity.Property(e => e.DateCreated).HasDefaultValueSql("GETUTCDATE()");
});


modelBuilder.Entity<ExperimentLaunch>(entity =>
{
    entity.HasIndex(e => e.PhETSimulationId);
    
    entity.HasOne(e => e.PhETSimulation)
          .WithMany(p => p.Launches)
          .HasForeignKey(e => e.PhETSimulationId)
          .OnDelete(DeleteBehavior.SetNull);
});


            modelBuilder.Entity<User>(b =>
            {
                b.HasIndex(u => u.Email).IsUnique();
                b.Property(u => u.GoogleSubject).HasMaxLength(255);
            });


            modelBuilder.Entity<EmailVerificationToken>(b =>
            {
                b.HasIndex(t => t.Token).IsUnique();
                b.HasOne(t => t.User)
                 .WithMany()
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<QuizAttempt>().Property(x => x.Score0to1).HasPrecision(5, 4);
            modelBuilder.Entity<Payment>().Property(p => p.PricePerStudent).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Total).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Vat).HasPrecision(18, 2);
            modelBuilder.Entity<Subscription>().Property(s => s.PricePerStudent).HasPrecision(18, 2);


            modelBuilder.Entity<PromoCode>(b =>
            {
                b.HasIndex(p => p.Code).IsUnique();
            });


            modelBuilder.Entity<Role>().HasData(
                new Role { Id = Guid.Parse("ae17f104-0ec3-47e3-9517-0e7e2c3be8b0"), Name = "Student" },
                new Role { Id = Guid.Parse("d7c51101-d2a4-40d5-bb0a-bd97898cf847"), Name = "Teacher" },
                new Role { Id = Guid.Parse("0187a77f-ea11-4f8c-87b5-03d3639adfbb"), Name = "SchoolAdmin" },
                new Role { Id = Guid.Parse("83b9ce68-4195-4c10-8e08-3dd6af2b0ec9"), Name = "GlobalAdmin" },
                new Role { Id = Guid.Parse("c1aaf5f0-f1d4-4c7e-a1b3-dbb1a8c92d89"), Name = "Parent" }
            );

            modelBuilder.Entity<School>().HasData(
                new School
                {
                    Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    Name = "Blue Sands Test School",
                    Subdomain = "bluesands-test",
                    IsActive = true,
                    DateCreated = new DateTime(2024, 08, 03, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("36a6c8c6-7f46-4043-939e-382dbb42db2b"),
                    FullName = "Ifedayo Michael",
                    Email = "ifemicheal2@gmail.com",
                    PasswordHash = "$2a$11$cEhjobe.nmtMXJHMXQWhW.a7HJrFSOdBhXdqYi2Oj4BYeTh9LXC2y",
                    RoleId = Guid.Parse("83b9ce68-4195-4c10-8e08-3dd6af2b0ec9"),
                    IsActive = true,
                    DateCreated = new DateTime(2024, 08, 03, 0, 0, 0, DateTimeKind.Utc)
                }
            );


            modelBuilder.Entity<PromoCode>().HasData(
                new PromoCode
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code = "ABIAEDTECH25",
                    IsActive = true,
                    ExpiresAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
                    MaxRedemptions = null,
                    RedemptionCount = 0
                }
            );





modelBuilder.Entity<PricingTier>()
    .Property(x => x.PricePerStudent)
    .HasColumnType("decimal(18,2)");

modelBuilder.Entity<PricingPromo>()
    .Property(x => x.PromoPricePerStudent)
    .HasColumnType("decimal(18,2)");

modelBuilder.Entity<Submission>()
    .Property(x => x.Score0to1)
    .HasPrecision(5,4);

modelBuilder.Entity<AuditEvent>()
    .Property(x => x.Value)
    .HasColumnType("decimal(18,4)");


            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.Category);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.DateCreated);
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<SupportTicket>(entity =>
            {
                entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.CreatedAt);
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<SupportResource>(entity =>
            {
                entity.HasIndex(x => x.Category);
            });


            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.HasIndex(x => x.Category);
                entity.HasIndex(x => x.IsActive);
            });


            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.CreatedAt);
                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }

        public override int SaveChanges()
        {
            TouchTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TouchTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void TouchTimestamps()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = now;
                    entry.Entity.DateModified = now;              
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DateModified = now;
                }
            }
        }
    }
}
