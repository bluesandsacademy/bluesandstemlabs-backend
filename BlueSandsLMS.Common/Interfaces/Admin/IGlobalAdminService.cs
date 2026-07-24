using System;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces.Admin
{
    public interface IGlobalAdminService
    {

        Task<GlobalAdminTotalsDto> GetTotalsAsync(CancellationToken ct = default);
        Task<GrowthSeriesDto> GetGrowthAsync(string metric, string period, int points, CancellationToken ct = default);
        Task<GeoUsageDto> GetGeoUsageAsync(CancellationToken ct = default);


        Task<PagedResult<GlobalAdminUserRowDto>> SearchUsersAsync(UserQuery query, CancellationToken ct = default);
        Task<GlobalAdminUserRowDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
        Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
        Task SetUserRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
        Task<ResetPasswordResponse> ResetPasswordAsync(Guid userId, CancellationToken ct = default);


        Task<PagedResult<PaymentRowDto>> GetPaymentsAsync(int page, int pageSize, CancellationToken ct = default);
        Task<PagedResult<SubscriptionRowDto>> GetSubscriptionsAsync(int page, int pageSize, CancellationToken ct = default);
        Task<RevenueBreakdownDto> GetRevenueBreakdownAsync(CancellationToken ct = default);


        Task<byte[]> ExportCsvAsync(GlobalExportRequest req, CancellationToken ct = default);


        Task<SupportOverviewDto> GetSupportOverviewAsync(CancellationToken ct = default);
        Task<PagedResult<SupportMessageDto>> GetSupportMessagesAsync(int page, int pageSize, CancellationToken ct = default);
    }
}
