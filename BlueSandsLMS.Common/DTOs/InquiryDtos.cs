
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Common.DTOs
{
    public class SchoolInquiryDto
    {
        [Required(ErrorMessage = "School name is required")]
        [StringLength(200)]
        public string SchoolName { get; set; } = null!;
        
        [Required(ErrorMessage = "School type is required")]
        public string Type { get; set; } = null!;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        public string Phone { get; set; } = null!;
        
        [Required(ErrorMessage = "Contact person name is required")]
        public string ContactPerson { get; set; } = null!;
        
        [Required(ErrorMessage = "Designation is required")]
        public string Designation { get; set; } = null!;
        
        [Range(1, 100000, ErrorMessage = "Student count must be between 1 and 100,000")]
        public int StudentCount { get; set; }
        
        [Range(1, 10000, ErrorMessage = "Teacher count must be between 1 and 10,000")]
        public int TeacherCount { get; set; }
    }

    public class IndividualInquiryDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;
        
        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; } = null!;
        
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = null!;
        
        [Required(ErrorMessage = "School name is required")]
        public string School { get; set; } = null!;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        public string Phone { get; set; } = null!;
        
        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = null!;
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
}