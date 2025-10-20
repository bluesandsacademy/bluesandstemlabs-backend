namespace BlueSandsLMS.Core.Common
{
    /// <summary>
    /// Minimal base: Id + created/modified timestamps (UTC).
    /// </summary>
    public abstract class BaseEntity
    {
        public long Id { get; set; }                 // EF identity column
        public DateTime DateCreated { get; set; }    // set on insert
        public DateTime DateModified { get; set; }   // set on insert/update
    }
}
