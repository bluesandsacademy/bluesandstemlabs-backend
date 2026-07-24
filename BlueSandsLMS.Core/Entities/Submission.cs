using System;
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class Submission
    {
        [Key] public Guid Id { get; set; }

        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
        public decimal? Score0to1 { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }


        public Guid? GraderUserId { get; set; }
        public string? Feedback { get; set; }


        public string? AttachmentUrl { get; set; }
        public string? AttachmentName { get; set; }
        public long? AttachmentSizeBytes { get; set; }
        public string? AttachmentContentType { get; set; }


        public Assignment? Assignment { get; set; }
        public User? Student { get; set; }
    }

    public enum SubmissionStatus { Pending = 0, Submitted = 1, Graded = 2 }
}
