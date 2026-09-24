using System;

namespace BlueSandsLMS.Common.DTOs.Admin
{
    public sealed class PlatformOverviewDto
    {
        public int TotalPlatformUsers { get; set; }
        public int activeUsers30d { get; set; }
        public int TotalPlatformStudents { get; set; }
        public int TotalSchoolsRegistered { get; set; }
        public int TotalSimulations { get; set; }
        public decimal TotalPayments { get; set; }
        public long TotalLabPracticeMinutes { get; set; }
        public long TotalExperimentAttempts { get; set; }
        public int IlsCreated { get; set; }
        public int TeachersCreatingIls { get; set; }
        public int IlsInDrafts { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
    }
}
