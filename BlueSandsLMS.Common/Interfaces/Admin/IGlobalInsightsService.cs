using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces.Admin
{

    public interface IGlobalInsightsService
    {

        Task<GlobalAiInsightsDto> GetAsync(CancellationToken ct = default);
    }
}