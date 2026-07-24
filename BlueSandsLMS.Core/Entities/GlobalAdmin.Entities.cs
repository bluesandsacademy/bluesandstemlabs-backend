using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{

    public class GeoLocation : BaseEntity
    {
        public Guid? UserId { get; set; }
        public Guid? SchoolId { get; set; }
        public string Country { get; set; } = "NG";
        public string? State { get; set; }
        public string? Lga { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public string Source { get; set; } = "login";
        public DateTime FirstSeenAtUtc { get; set; }
        public DateTime LastSeenAtUtc { get; set; }
    }

    public class UsageEvent : BaseEntity
    {
        public Guid? UserId { get; set; }
        public Guid? SchoolId { get; set; }
        public string EventType { get; set; } = default!;
        public Guid? SubjectId { get; set; }
        public string? MetaJson { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }


    public class ApprovalQueue : BaseEntity
    {
        public string Type { get; set; } = default!;
        public Guid EntityId { get; set; }
        public Guid SubmittedBy { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public string Status { get; set; } = "Pending";
        public Guid? ReviewedBy { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? Notes { get; set; }
    }

    

   

    public class SystemSetting : BaseEntity
    {
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string Type { get; set; } = "string";
        public Guid UpdatedBy { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class FeatureFlag : BaseEntity
    {
        public string Key { get; set; } = default!;
        public bool IsEnabled { get; set; }
        public string Audience { get; set; } = "All";
        public string? Notes { get; set; }
    }

    public class PromoWindow : BaseEntity
    {
        public string Name { get; set; } = default!;
        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public decimal DiscountPercent { get; set; }
        public string AppliesToPlan { get; set; } = "School";
        public bool IsActive { get; set; }
    }


    public class RevenueMonthlyAgg
    {
        public string YearMonth { get; set; } = default!;
        public string Currency { get; set; } = "NGN";
        public decimal Gross { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public decimal Mrr { get; set; }
        public int ActiveSubs { get; set; }
        public int NewSubs { get; set; }
        public int ChurnedSubs { get; set; }
    }

    public class TeacherMetricsView
    {
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = default!;
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = default!;
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public decimal Score { get; set; }
    }

    public class SchoolMetricsView
    {
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = default!;
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public decimal Score { get; set; }
    }
}
