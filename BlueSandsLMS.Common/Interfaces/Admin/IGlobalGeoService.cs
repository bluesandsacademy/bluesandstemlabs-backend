using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces.Admin
{

    public interface IGlobalGeoService
    {

        Task<GeoAdvancedDto> GetAsync(
            string scope, 
            string? country, 
            string? state, 
            CancellationToken ct = default);
    }
}