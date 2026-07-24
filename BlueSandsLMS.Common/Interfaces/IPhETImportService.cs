using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IPhETImportService
    {
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream, CancellationToken ct = default);
        Task<ImportResult> ImportFromFileAsync(string path, CancellationToken ct = default);
    }
}
