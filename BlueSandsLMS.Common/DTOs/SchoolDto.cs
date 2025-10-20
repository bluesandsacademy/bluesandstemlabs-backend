namespace BlueSandsLMS.Common.DTOs
{
    public class SchoolDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Subdomain { get; set; } = null!;
    }
}