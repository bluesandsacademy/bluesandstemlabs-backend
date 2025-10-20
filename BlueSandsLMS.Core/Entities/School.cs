namespace BlueSandsLMS.Core.Entities
{
    public class School
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Subdomain { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }

        // New: management & analytics fields
        public string Country { get; set; } = string.Empty;
        public int TotalStudents { get; set; }

        // Contact
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactPosition { get; set; } = string.Empty;

        public ICollection<User>? Users { get; set; }
    }
}
