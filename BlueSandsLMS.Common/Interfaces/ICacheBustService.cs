namespace BlueSandsLMS.Common.Interfaces
{
    public interface ICacheBustService
    {
        void InvalidateSchoolAdmin(Guid schoolId);
        void InvalidateUser(Guid userId);
        // add others if you use them
    }
}
