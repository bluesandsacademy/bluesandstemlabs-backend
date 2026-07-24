using System.ComponentModel.DataAnnotations;



namespace BlueSandsLMS.Core.Entities
{
    public class School
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Subdomain { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }


        public string Country { get; set; } = string.Empty;
        public string Currency { get; set; } = "NGN";
        public int TotalStudents { get; set; }


        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactPosition { get; set; } = string.Empty;

        [MaxLength(100)] public string? State { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(100)] public string? Lga { get; set; }


        public int? EstimatedScienceStudentCount { get; set; }
        public int? DisabledStudentCount { get; set; }

        public ICollection<User>? Users { get; set; }
    }
}
