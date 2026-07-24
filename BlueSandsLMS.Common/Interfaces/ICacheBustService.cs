using System;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface ICacheBustService
    {
        void InvalidateSchoolAdmin(Guid schoolId);
        void InvalidateGlobal();
    }
}
