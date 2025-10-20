using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services
{
    public class ExportService : IExportService
    {
        private readonly BlueSandsLMSDbContext _db;
        public ExportService(BlueSandsLMSDbContext db) => _db = db;

        public async Task<byte[]> ExportGradebookCsvAsync(Guid classId)
        {
            var rows = await (
                from a in _db.Assignments
                where a.ClassroomId == classId
                join s in _db.Submissions on a.Id equals s.AssignmentId into gj
                from s in gj.DefaultIfEmpty()
                join u in _db.Users on s.StudentId equals u.Id into uj   // ✅ use StudentId
                from u in uj.DefaultIfEmpty()
                orderby a.DueAt, u.FullName
                select new
                {
                    ClassId = classId,
                    AssignmentTitle = a.Title,
                    DueAt = a.DueAt,
                    Student = u.FullName,
                    StudentEmail = u.Email,
                    Status = s == null ? "NotSubmitted" : s.Status.ToString(),
                    // ✅ export percentage; if null, stays null
                    ScorePercent = s == null ? (decimal?)null : (s.Score0to1 * 100m),
                    SubmittedAt = s == null ? (DateTime?)null : s.SubmittedAt,
                    GradedAt = s == null ? (DateTime?)null : s.GradedAt
                }
            ).ToListAsync();

            var sb = new StringBuilder();
            Header(sb, "ClassId,AssignmentTitle,DueAt,Student,StudentEmail,Status,ScorePercent,SubmittedAt,GradedAt");
            foreach (var r in rows)
            {
                Line(sb,
                    r.ClassId,
                    r.AssignmentTitle,
                    r.DueAt,
                    r.Student,
                    r.StudentEmail,
                    r.Status,
                    r.ScorePercent,
                    r.SubmittedAt,
                    r.GradedAt);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportUsersCsvAsync(Guid schoolId)
        {
            var rows = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.SchoolId == schoolId)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    Role = u.Role != null ? u.Role.Name : "",
                    u.IsEmailVerified,
                    u.LastLogin,
                    u.DateCreated
                }).ToListAsync();

            var sb = new StringBuilder();
            Header(sb, "UserId,FullName,Email,Role,IsEmailVerified,LastLogin,DateCreated");
            foreach (var r in rows)
                Line(sb, r.Id, r.FullName, r.Email, r.Role, r.IsEmailVerified, r.LastLogin, r.DateCreated);

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportActivityCsvAsync(Guid schoolId, DateTime fromUtc, DateTime toUtc)
        {
            var userIds = await _db.Users
                .Where(u => u.SchoolId == schoolId)
                .Select(u => u.Id)
                .ToListAsync();

            var experiments = await _db.ExperimentLaunches
                .Where(e => userIds.Contains(e.UserId) && e.StartedAt >= fromUtc && e.StartedAt <= toUtc)
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count(), Mins = g.Sum(x => (int?)x.DurationSec) / 60 })
                .ToListAsync();

            var quizzes = await _db.QuizAttempts
    .Where(q => userIds.Contains(q.UserId) && q.CompletedAt >= fromUtc && q.CompletedAt <= toUtc)
    .GroupBy(q => q.UserId)
    .Select(g => new
    {
        UserId   = g.Key,
        Attempts = g.Count(),
        // Force nullable average, then coalesce
        Avg      = (g.Average(x => (decimal?)x.Score0to1) ?? 0m) * 100m
    })
    .ToListAsync();


            var users = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            var sb = new StringBuilder();
            Header(sb, "UserId,FullName,Email,Experiments,TimeMins,QuizAttempts,AvgQuizScore");
            foreach (var u in users)
            {
                var e = experiments.FirstOrDefault(x => x.UserId == u.Id);
                var q = quizzes.FirstOrDefault(x => x.UserId == u.Id);
                Line(sb, u.Id, u.FullName, u.Email,
                    e?.Count ?? 0,
                    e?.Mins ?? 0,
                    q?.Attempts ?? 0,
                    q?.Avg is decimal d ? Math.Round(d, 2) : 0m);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // CSV helpers
        private static void Header(StringBuilder sb, string header) => sb.AppendLine(header);
        private static void Line(StringBuilder sb, params object?[] cols)
        {
            sb.AppendLine(string.Join(",", cols.Select(Csv)));
        }
        private static string Csv(object? o)
        {
            if (o == null) return "";
            var s = o is DateTime dt ? dt.ToString("u") : o.ToString() ?? "";
            s = s.Replace("\"", "\"\"");
            return s.Contains(',') || s.Contains('\n') ? $"\"{s}\"" : s;
        }
    }
}
