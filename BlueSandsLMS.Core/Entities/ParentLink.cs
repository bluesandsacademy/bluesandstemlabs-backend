namespace BlueSandsLMS.Core.Entities
{
    public class ParentLink
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string ParentEmail { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
