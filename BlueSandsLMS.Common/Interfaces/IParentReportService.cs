using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IParentReportService
    {

        Task<int> SendMonthlyReportsAsync(Guid schoolId, int year, int month, CancellationToken ct = default);
    }
}
