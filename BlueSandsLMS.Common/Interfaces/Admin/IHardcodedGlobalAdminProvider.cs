using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces.Admin;

public interface IHardcodedGlobalAdminProvider
{
    Task<PromptTotalsDto> GetPromptTotalsAsync(CancellationToken ct = default);
}