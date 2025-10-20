// BlueSandsLMS.Infrastructure/Setup/DbSeeder.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Infrastructure.Setup
{
    public static class DbSeeder
    {
        public static async Task SeedAllAsync(
            BlueSandsLMSDbContext db,
            bool seedDemoLessons,
            bool seedTeacherStarterPack,
            CancellationToken ct = default)
        {
            await SeedSubjectsAsync(db, ct);

            if (seedDemoLessons)
                await SeedDemoLessonsAsync(db, ct);

            if (seedTeacherStarterPack)
                await SeedTeacherStarterPackAsync(db, ct);
        }

        // ---- (1) Baseline: Subjects only (idempotent) ----
        public static async Task SeedSubjectsAsync(BlueSandsLMSDbContext db, CancellationToken ct = default)
        {
            var subjects = new[]
            {
                new Subject { Code = "PHY",  Name = "Physics",     IsActive = true, SortOrder = 1 },
                new Subject { Code = "CHEM", Name = "Chemistry",   IsActive = true, SortOrder = 2 },
                new Subject { Code = "BIO",  Name = "Biology",     IsActive = true, SortOrder = 3 },
                new Subject { Code = "MATH", Name = "Mathematics", IsActive = true, SortOrder = 4 },
            };

            foreach (var s in subjects)
            {
                var existing = await db.Subjects.FirstOrDefaultAsync(x => x.Code == s.Code, ct);
                if (existing == null)
                    db.Subjects.Add(s);
                else
                {
                    existing.Name = s.Name;
                    existing.IsActive = s.IsActive;
                    existing.SortOrder = s.SortOrder;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        // ---- (2) Dev Demo: minimal lessons so UI works now ----
        // Safe-guard: only seeds if *no* lessons exist yet.
        public static async Task SeedDemoLessonsAsync(BlueSandsLMSDbContext db, CancellationToken ct = default)
        {
            var any = await db.Lessons.AnyAsync(ct);
            if (any) return;

            db.Lessons.Add(new Lesson
            {
                Id          = Guid.NewGuid(),
                SubjectCode = "PHY",
                Title       = "Motion & Vectors (Demo)",
                Summary     = "Intro to motion",
                DurationMin = 15,
                SortOrder   = 1,
                IsActive    = true
            });

            db.Lessons.Add(new Lesson
            {
                Id          = Guid.NewGuid(),
                SubjectCode = "CHEM",
                Title       = "Lab Safety (Demo)",
                Summary     = "Safety rules and PPE basics",
                DurationMin = 10,
                SortOrder   = 1,
                IsActive    = true
            });

            await db.SaveChangesAsync(ct);
        }

        // ---- (3) Teacher Starter Pack (OFF by default) ----
        // A richer seed you can switch on for staging/launch day so teachers aren’t starting from zero.
        // Idempotent: only creates if subject has <= N lessons.
        public static async Task SeedTeacherStarterPackAsync(BlueSandsLMSDbContext db, CancellationToken ct = default)
        {
            await EnsureLessonAsync(db, "PHY", 1, "Kinematics Basics", "Displacement, velocity, acceleration", 20, ct);
            await EnsureLessonAsync(db, "PHY", 2, "Newton’s Laws", "First, Second, Third law overview", 25, ct);

            await EnsureLessonAsync(db, "CHEM", 1, "Atomic Structure", "Protons, neutrons, electrons", 18, ct);
            await EnsureLessonAsync(db, "BIO",  1, "Cell Structure", "Prokaryotes vs eukaryotes", 18, ct);
            await EnsureLessonAsync(db, "MATH", 1, "Algebra Refresher", "Expressions and equations", 22, ct);
        }

        private static async Task EnsureLessonAsync(
            BlueSandsLMSDbContext db,
            string subjectCode,
            int sortOrder,
            string title,
            string? summary,
            int durationMin,
            CancellationToken ct)
        {
            var exists = await db.Lessons.AnyAsync(
                l => l.SubjectCode == subjectCode && l.SortOrder == sortOrder && l.Title == title, ct);

            if (exists) return;

            db.Lessons.Add(new Lesson
            {
                Id          = Guid.NewGuid(),
                SubjectCode = subjectCode,
                Title       = title,
                Summary     = summary,
                DurationMin = durationMin,
                SortOrder   = sortOrder,
                IsActive    = true
            });

            await db.SaveChangesAsync(ct);
        }
    }
}
