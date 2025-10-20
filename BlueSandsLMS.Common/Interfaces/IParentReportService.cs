using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IParentReportService
    {
        /// <summary>
        /// Sends monthly student progress reports to parent emails linked under ParentLinks.
        /// </summary>
        /// <returns>Total emails attempted (sent).</returns>
        Task<int> SendMonthlyReportsAsync(Guid schoolId, int year, int month, CancellationToken ct = default);
    }
}
