using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IPhETSeedDataService
    {
        Task<SeedResult> GenerateSeedDataAsync(int studentCount, int experimentsPerStudent, CancellationToken ct = default);
    }
}