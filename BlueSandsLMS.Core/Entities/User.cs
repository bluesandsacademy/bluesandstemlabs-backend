namespace BlueSandsLMS.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
public string? Gender { get; set; } 
        public string? Country { get; set; }

        public string PasswordHash { get; set; } = null!;
        public string? GoogleSubject { get; set; }
        public Guid? SchoolId { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? LastLogin { get; set; }

        public School? School { get; set; }
        public Role? Role { get; set; }

      
public bool IsEmailVerified { get; set; } = false;
public DateTime? EmailVerifiedAt { get; set; }

    }
}
