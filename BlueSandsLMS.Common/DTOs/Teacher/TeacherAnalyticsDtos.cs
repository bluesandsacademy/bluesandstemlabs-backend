using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.Teacher
{
    // ---------- Overview ----------
    public sealed class TeacherOverviewDto
    {
        public int ActiveStudentsToday { get; set; }
        public int ActiveStudents7d { get; set; }
        public int ActiveStudents30d { get; set; }
        public int TimeSpentSec7d { get; set; }
        public double LessonCompletionRate { get; set; } // 0..1
        public double AvgScorePercent { get; set; }      // 0..100
        public LabeledCount[] MostAttemptedLabs { get; set; } = Array.Empty<LabeledCount>();
        public GenderSplit ClassGenderSplit { get; set; } = new();
    }

    public sealed class LabeledCount
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    public sealed class GenderSplit
    {
        public double Male { get; set; }
        public double Female { get; set; }
        public double Other { get; set; }
    }

    public sealed class LabeledShare
    {
        public string Label { get; set; } = "";
        public double Share { get; set; }
    }

    // ---------- Engagement ----------
    public sealed class TeacherEngagementDto
    {
        public StudentEngagement[] ByStudent { get; set; } = Array.Empty<StudentEngagement>();
        public HourBucket[] PeakUsage { get; set; } = Array.Empty<HourBucket>();

        // Optional maps for UI display (names/avatars/etc.)
        public Dictionary<Guid, string>? DisplayNames { get; set; }

        // Placeholders for future telemetry (not populated yet)
        public LabeledShare[] DeviceMix { get; set; } = Array.Empty<LabeledShare>();
        public LabeledShare[] BrowserMix { get; set; } = Array.Empty<LabeledShare>();
    }

    public sealed class StudentEngagement
    {
        public Guid StudentId { get; set; }
        public int TimeSpentSec { get; set; }
        public int Interactions { get; set; }           // launches + quiz attempts (approx)
        public double LessonCompletionRate { get; set; } // 0..1
    }

    public sealed class HourBucket
    {
        public int Hour { get; set; }   // 0..23
        public int Count { get; set; }
    }

    // ---------- Performance ----------
    public sealed class TeacherPerformanceDto
    {
        public double AvgScorePercent { get; set; }
        public DayPoint[] Trend { get; set; } = Array.Empty<DayPoint>();
        public StudentScore[] TopPerformers { get; set; } = Array.Empty<StudentScore>();
        public AtRiskStudent[] AtRisk { get; set; } = Array.Empty<AtRiskStudent>();

        // Optional map for UI display
        public Dictionary<Guid, string>? DisplayNames { get; set; }
    }

    public sealed class DayPoint
    {
        public DateOnly Date { get; set; }
        public double Avg { get; set; }
    }

    public sealed class StudentScore
    {
        public Guid StudentId { get; set; }
        public double AvgPercent { get; set; }
        public int Attempts { get; set; }
    }

    public sealed class AtRiskStudent
    {
        public Guid StudentId { get; set; }
        public string[] Reason { get; set; } = Array.Empty<string>(); // "Inactivity7d", "LowScore"
    }

    // ---------- Assignments ----------
    public sealed class TeacherAssignmentsDto
    {
        public int Created { get; set; }
        public int Submitted { get; set; }
        public double LatePct { get; set; }
        public double TurnaroundMsAvg { get; set; }
        public AssignmentLine[] ByAssignment { get; set; } = Array.Empty<AssignmentLine>();
    }

    public sealed class AssignmentLine
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime? DueAt { get; set; }
        public int Submitted { get; set; }
        public int Late { get; set; }
        public double AvgScorePercent { get; set; }
    }

    // ---------- Attendance ----------
    public sealed class TeacherAttendanceDto
    {
        public int OnlineNow { get; set; }    // approx
        public int ActiveToday { get; set; }  // approx
        public DayActive[] ByDay { get; set; } = Array.Empty<DayActive>();
    }

    public sealed class DayActive
    {
        public DateOnly Date { get; set; }
        public int Active { get; set; }
    }
}
