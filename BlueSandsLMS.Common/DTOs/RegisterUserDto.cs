namespace BlueSandsLMS.Common.DTOs
{
    public class RegisterUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }



    public string? CouponCode { get; set; }
}

}
