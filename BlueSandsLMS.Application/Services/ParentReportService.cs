using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services
{
    public class ParentReportService : IParentReportService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IEmailService _email;

        public ParentReportService(BlueSandsLMSDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        public async Task<int> SendMonthlyReportsAsync(Guid schoolId, int year, int month, CancellationToken ct = default)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1).AddTicks(-1);
            var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);


            var students = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.SchoolId == schoolId && u.Role != null && u.Role.Name == "Student")
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync(ct);


            var studentIds = students.Select(s => s.Id).ToList();
            var parentLinks = await _db.ParentLinks
                .Where(p => studentIds.Contains(p.StudentId))
                .GroupBy(p => p.StudentId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x).ToList(), ct);

            int emails = 0;

            foreach (var s in students)
            {
                if (!parentLinks.TryGetValue(s.Id, out var links) || links.Count == 0)
                    continue;


                var experimentsCompleted = await _db.ExperimentLaunches
                    .Where(x => x.UserId == s.Id && x.Completed && x.StartedAt >= start && x.StartedAt <= end)
                    .CountAsync(ct);

                var timeMins = (await _db.ExperimentLaunches
                    .Where(x => x.UserId == s.Id && x.StartedAt >= start && x.StartedAt <= end)
                    .SumAsync(x => (int?)x.DurationSec, ct) ?? 0) / 60;

                var avgQuiz = await _db.QuizAttempts
                    .Where(q => q.UserId == s.Id && q.CompletedAt >= start && q.CompletedAt <= end)
                    .AverageAsync(q => (decimal?)q.Score0to1, ct) ?? 0m;

                var badges = await _db.BadgeAwards.Where(b => b.UserId == s.Id && b.AwardedAt >= start && b.AwardedAt <= end).CountAsync(ct);


                var html = BuildMonthlyHtml(s.FullName ?? "Student", monthName, year, experimentsCompleted, timeMins, Math.Round(avgQuiz * 100m, 2), badges);

                foreach (var link in links)
                {
                    var subject = $"Blue Sands STEM Labs – {s.FullName}: {monthName} {year} Progress Report";
                    await _email.SendAsync(link.ParentEmail, subject, html);
                    emails++;
                }
            }

            return emails;
        }

        private static string BuildMonthlyHtml(string studentName, string monthName, int year,
            int experimentsCompleted, int timeMins, decimal avgQuizPercent, int badges)
        {
            return $@"
<!doctype html><html><body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6"">
<h2>Monthly Progress Report – {monthName} {year}</h2>
<p>Dear Parent/Guardian,</p>
<p>Here is the {monthName} performance summary for <strong>{System.Net.WebUtility.HtmlEncode(studentName)}</strong> on Blue Sands STEM Labs.</p>
<table style=""border-collapse:collapse"">
  <tr><td style=""padding:6px 12px"">Experiments Completed</td><td style=""padding:6px 12px""><strong>{experimentsCompleted}</strong></td></tr>
  <tr><td style=""padding:6px 12px"">Time Spent in Labs</td><td style=""padding:6px 12px""><strong>{timeMins} mins</strong></td></tr>
  <tr><td style=""padding:6px 12px"">Average Quiz Score</td><td style=""padding:6px 12px""><strong>{avgQuizPercent}%</strong></td></tr>
  <tr><td style=""padding:6px 12px"">Badges Earned</td><td style=""padding:6px 12px""><strong>{badges}</strong></td></tr>
</table>
<p>Thank you for supporting your learner. For any questions, reply to this email or contact the school admin.</p>
<p>— Blue Sands STEM Labs</p>
</body></html>";
        }
    }
}
