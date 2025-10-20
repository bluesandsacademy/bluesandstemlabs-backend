using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class BadgeAward
    {
        [Key] public Guid Id { get; set; }
        public Guid UserId { get; set; }
        [MaxLength(100)] public string BadgeCode { get; set; } = "";
         public string Code { get; set; } = "";         // FIRST_LAUNCH, SCORE_90_PLUS
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
