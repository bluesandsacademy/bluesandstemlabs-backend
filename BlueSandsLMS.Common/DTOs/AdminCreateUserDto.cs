
namespace BlueSandsLMS.Common.DTOs
{
    public class AdminCreateUserDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Guid RoleId { get; set; }
        public Guid SchoolId { get; set; }

        public string? CouponCode { get; set; }
    }
}
