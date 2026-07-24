namespace BlueSandsLMS.Common.DTOs
{
    public class RegisterSchoolDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Position { get; set; } = null!;
        public int TotalStudents { get; set; }
        public string Country { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? CouponCode { get; set; }
        public string SchoolName { get; set; } = null!;
        public string? Subdomain { get; set; }


        public int? EstimatedScienceStudentCount { get; set; }
        public int? DisabledStudentCount { get; set; }
    }
}
