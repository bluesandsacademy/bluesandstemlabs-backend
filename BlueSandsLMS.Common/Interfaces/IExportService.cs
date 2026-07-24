using System;
using System.Threading.Tasks;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportGradebookCsvAsync(Guid classId);
        Task<byte[]> ExportUsersCsvAsync(Guid schoolId);
        Task<byte[]> ExportActivityCsvAsync(Guid schoolId, DateTime fromUtc, DateTime toUtc);

        Task<byte[]> ExportEngagementCsvAsync(Guid teacherId, Guid? classroomId, DateTime fromUtc, DateTime toUtc);
    }
}
