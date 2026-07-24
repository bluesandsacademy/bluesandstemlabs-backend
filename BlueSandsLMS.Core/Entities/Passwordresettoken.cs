using System;

namespace BlueSandsLMS.Core.Entities
{

    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        

        public string TokenHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        

        public bool IsUsed { get; set; }
        

        public virtual User User { get; set; } = null!;
    }
}