using System.ComponentModel.DataAnnotations;
using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public enum FeedbackCategory { General = 0, Bug = 1, FeatureRequest = 2, Content = 3 }
    public enum FeedbackStatus  { Pending = 0, Reviewed = 1, Dismissed = 2 }

    public class Feedback : BaseEntity
    {
        public Guid UserId { get; set; }

        [MaxLength(50)] public string UserType { get; set; } = "";

        [Required, MaxLength(1000)] public string Message { get; set; } = "";

        public FeedbackCategory Category { get; set; } = FeedbackCategory.General;
        public FeedbackStatus   Status   { get; set; } = FeedbackStatus.Pending;

        public User? User { get; set; }
    }
}
