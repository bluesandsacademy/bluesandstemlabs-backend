namespace BlueSandsLMS.Core.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!; // e.g., "Student", "Teacher", etc.
        public ICollection<User>? Users { get; set; }
    }
}
