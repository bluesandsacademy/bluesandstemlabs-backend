using System;
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Common
{

    public abstract class BaseEntity
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime DateCreated { get; set; }  = DateTime.UtcNow;
        public DateTime DateModified { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
