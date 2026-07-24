using System;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces.Admin
{
    public interface IGlobalTicketService
    {
        Task<PagedResult<TicketRowDto>> SearchAsync(TicketQuery query, CancellationToken ct = default);
        Task<TicketDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<Guid> CreateAsync(CreateTicketRequest req, CancellationToken ct = default);
        Task UpdateAsync(Guid id, UpdateTicketRequest req, CancellationToken ct = default);
        Task AddCommentAsync(Guid id, AddTicketCommentRequest req, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
