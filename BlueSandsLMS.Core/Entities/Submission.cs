using System;
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class Submission
    {
        [Key] public Guid Id { get; set; }

        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending; // Pending|Submitted|Graded
        public decimal? Score0to1 { get; set; } // 0..1
        public DateTime? SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }

        // --- Grading & feedback ---
        public Guid? GraderUserId { get; set; }
        public string? Feedback { get; set; }

        // --- Simple attachment placeholders (no storage yet) ---
        public string? AttachmentUrl { get; set; }
        public string? AttachmentName { get; set; }
        public long? AttachmentSizeBytes { get; set; }
        public string? AttachmentContentType { get; set; }

        // navs
        public Assignment? Assignment { get; set; }
        public User? Student { get; set; }
    }

    public enum SubmissionStatus { Pending = 0, Submitted = 1, Graded = 2 }
}
