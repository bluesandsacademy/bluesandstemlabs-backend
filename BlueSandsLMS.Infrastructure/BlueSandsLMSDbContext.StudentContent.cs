using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlueSandsLMS.Core.Entities;

namespace BlueSandsLMS.Infrastructure
{

    public partial class BlueSandsLMSDbContext
    {
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
        public DbSet<Certificate> Certificates => Set<Certificate>();

        partial void OnModelCreatingStudentContent(ModelBuilder modelBuilder)
        {
            ConfigureSubject(modelBuilder.Entity<Subject>());
            ConfigureLesson(modelBuilder.Entity<Lesson>());
            ConfigureLessonProgress(modelBuilder.Entity<LessonProgress>());
            ConfigureCertificate(modelBuilder.Entity<Certificate>());
        }

        private static void ConfigureSubject(EntityTypeBuilder<Subject> e)
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(16);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.IsActive).HasDefaultValue(true);

            e.HasMany(x => x.Lessons)
             .WithOne(x => x.Subject!)
             .HasForeignKey(x => x.SubjectCode)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.SortOrder);
        }

        private static void ConfigureLesson(EntityTypeBuilder<Lesson> e)
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubjectCode).HasMaxLength(16);
            e.Property(x => x.Title).HasMaxLength(256);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasIndex(x => new { x.SubjectCode, x.IsActive, x.SortOrder });
        }

        private static void ConfigureLessonProgress(EntityTypeBuilder<LessonProgress> e)
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();

            e.HasOne(x => x.Lesson!)
             .WithMany(x => x.Progresses)
             .HasForeignKey(x => x.LessonId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User!)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureCertificate(EntityTypeBuilder<Certificate> e)
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubjectCode).HasMaxLength(16);
            e.Property(x => x.Title).HasMaxLength(256);

            e.HasOne(x => x.User!)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Subject!)
             .WithMany()
             .HasForeignKey(x => x.SubjectCode)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.UserId, x.SubjectCode, x.IssuedAt });
        }
    }
}
