using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Infrastructure
{
    // ⬅️ MUST be partial so we can implement pieces in other files
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
        public DbSet<MessageLog> MessageLogs { get; set; }
        public DbSet<ForumTopic> ForumTopics { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }

        public DbSet<BadgeAward> BadgeAwards => Set<BadgeAward>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

        public DbSet<PricingTier> PricingTiers => Set<PricingTier>();
        public DbSet<PricingPromo> PricingPromos => Set<PricingPromo>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();

         partial void OnModelCreatingStudentContent(ModelBuilder modelBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Student Content (Subjects/Lessons/Progress/Certificates) ----------
            OnModelCreatingStudentContent(modelBuilder); // implemented in the partial file below

            // ---------- Indexes & relationships (existing) ----------
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

            // Users
            modelBuilder.Entity<User>(b =>
            {
                b.HasIndex(u => u.Email).IsUnique();
            });

            // EmailVerificationTokens
            modelBuilder.Entity<EmailVerificationToken>(b =>
            {
                b.HasIndex(t => t.Token).IsUnique();
                b.HasOne(t => t.User)
                 .WithMany()
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Decimal precisions (silence warnings + prevent truncation)
            modelBuilder.Entity<QuizAttempt>().Property(x => x.Score0to1).HasPrecision(5, 4);
            modelBuilder.Entity<Payment>().Property(p => p.PricePerStudent).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Total).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Vat).HasPrecision(18, 2);
            modelBuilder.Entity<Subscription>().Property(s => s.PricePerStudent).HasPrecision(18, 2);

            // PromoCodes
            modelBuilder.Entity<PromoCode>(b =>
            {
                b.HasIndex(p => p.Code).IsUnique();
            });

            // ---------- Seeds (static, deterministic) ----------
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
                    PasswordHash = "$2a$11$cEhjobe.nmtMXJHMXQWhW.a7HJrFSOdBhXdqYi2Oj4BYeTh9LXC2y", // Nisotgreg0
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

            // using Microsoft.EntityFrameworkCore;

modelBuilder.Entity<PricingTier>()
    .Property(x => x.PricePerStudent)
    .HasColumnType("decimal(18,2)");

modelBuilder.Entity<PricingPromo>()
    .Property(x => x.PromoPricePerStudent)
    .HasColumnType("decimal(18,2)");

modelBuilder.Entity<Submission>()
    .Property(x => x.Score0to1)
    .HasPrecision(5,4); // scores like 0.8234

modelBuilder.Entity<AuditEvent>()
    .Property(x => x.Value)
    .HasColumnType("decimal(18,4)");

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
