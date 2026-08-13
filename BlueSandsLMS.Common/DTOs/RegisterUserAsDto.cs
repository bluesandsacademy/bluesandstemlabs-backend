namespace BlueSandsLMS.Common.DTOs;

public class RegisterUserAsDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string RoleName { get; set; } = null!;     // "Student", "Teacher", "SchoolAdmin", "GlobalAdmin", "Parent", "Admin"
    public Guid SchoolId { get; set; } = Guid.Empty;   // optional except for SchoolAdmin
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? CouponCode { get; set; }
}