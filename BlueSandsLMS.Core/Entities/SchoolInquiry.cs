
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class SchoolInquiry
    {
        public Guid Id { get; set; }
        
        [Required, MaxLength(200)]
        public string SchoolName { get; set; } = null!;
        
        [Required, MaxLength(50)]
        public string Type { get; set; } = null!;
        
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = null!;
        
        [Required, MaxLength(20)]
        public string Phone { get; set; } = null!;
        
        [Required, MaxLength(100)]
        public string ContactPerson { get; set; } = null!;
        
        [Required, MaxLength(100)]
        public string Designation { get; set; } = null!;
        
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsContacted { get; set; }
    }

    public class IndividualInquiry
    {
        public Guid Id { get; set; }
        
        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;
        
        [Required, MaxLength(20)]
        public string Gender { get; set; } = null!;
        
        [Required, MaxLength(50)]
        public string Role { get; set; } = null!;
        
        [Required, MaxLength(200)]
        public string School { get; set; } = null!;
        
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = null!;
        
        [Required, MaxLength(20)]
        public string Phone { get; set; } = null!;
        
        [Required, MaxLength(100)]
        public string Location { get; set; } = null!;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public DateTime DateCreated { get; set; }
        public bool IsContacted { get; set; }
    }
}