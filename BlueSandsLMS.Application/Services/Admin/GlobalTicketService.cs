using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;
using BlueSandsLMS.Infrastructure;
using TicketEntity = BlueSandsLMS.Core.Entities.Ticket;
using TicketCommentEntity = BlueSandsLMS.Core.Entities.TicketComment;
using BlueSandsLMS.Core.Entities;

namespace BlueSandsLMS.Application.Services.Admin
{
    public sealed class GlobalTicketService : IGlobalTicketService
    {
        private readonly BlueSandsLMSDbContext _db;
        public GlobalTicketService(BlueSandsLMSDbContext db) => _db = db;

        private IQueryable<TicketEntity> BaseQuery()
            => _db.Set<TicketEntity>().AsNoTracking()
                 .Include(t => t.School)
                 .Include(t => t.CreatedByUser)
                 .Include(t => t.AssignedToUser);

        public async Task<PagedResult<TicketRowDto>> SearchAsync(TicketQuery query, CancellationToken ct = default)
        {
            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;

            var q = BaseQuery();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var needle = query.Q.ToLower();
                q = q.Where(t =>
                    (t.Subject ?? "").ToLower().Contains(needle) ||
                    (t.Body ?? "").ToLower().Contains(needle) ||
                    ((t.TagsCsv ?? "").ToLower().Contains(needle))
                );
            }
            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TicketStatus>(query.Status, true, out var st))
                q = q.Where(t => t.Status == st);
            if (!string.IsNullOrWhiteSpace(query.Priority) && Enum.TryParse<TicketPriority>(query.Priority, true, out var pr))
                q = q.Where(t => t.Priority == pr);
            if (query.SchoolId.HasValue) q = q.Where(t => t.SchoolId == query.SchoolId);
            if (query.AssignedToUserId.HasValue) q = q.Where(t => t.AssignedToUserId == query.AssignedToUserId);

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(t => t.UpdatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t => new TicketRowDto(
                    t.Id,
                    t.Subject,
                    (t.Body != null && t.Body.Length > 120) ? t.Body.Substring(0, 120) + "…" : (t.Body ?? ""),
                    t.Status.ToString(),
                    t.Priority.ToString(),
                    t.SchoolId,
                    t.School != null ? t.School.Name : null,
                    t.CreatedByUserId,
                    t.CreatedByUser != null ? t.CreatedByUser.FullName : "",
                    t.AssignedToUserId,
                    t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                    t.CreatedAt, t.UpdatedAt, t.TagsCsv
                ))
                .ToListAsync(ct);

            return new PagedResult<TicketRowDto>(query.Page, query.PageSize, total, items);
        }

        public async Task<TicketDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var t = await _db.Set<TicketEntity>()
                .Include(x => x.School)
                .Include(x => x.CreatedByUser)
                .Include(x => x.AssignedToUser)
                .Include(x => x.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t == null) return null;

            var comments = t.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new TicketCommentDto(
                    c.Id, c.UserId, c.User != null ? c.User.FullName : "", c.Body, c.IsPrivate, c.CreatedAt))
                .ToList();

            return new TicketDetailDto(
                t.Id, t.Subject, t.Body ?? "",
                t.Status.ToString(), t.Priority.ToString(), t.Source.ToString(),
                t.SchoolId, t.School?.Name,
                t.CreatedByUserId, t.CreatedByUser?.FullName ?? "",
                t.AssignedToUserId, t.AssignedToUser?.FullName,
                t.CreatedAt, t.UpdatedAt, t.ClosedAt,
                t.TagsCsv, comments
            );
        }

        public async Task<Guid> CreateAsync(CreateTicketRequest req, CancellationToken ct = default)
        {
            if (!Enum.TryParse<TicketPriority>(req.Priority ?? "Medium", true, out var pr)) pr = TicketPriority.Medium;
            if (!Enum.TryParse<TicketSource>(req.Source ?? "System", true, out var src)) src = TicketSource.System;

            var t = new TicketEntity
            {
                SchoolId = req.SchoolId,
                CreatedByUserId = req.CreatedByUserId,
                AssignedToUserId = req.AssignedToUserId,
                Subject = (req.Subject ?? "").Trim(),
                Body = (req.Body ?? "").Trim(),
                Priority = pr,
                Source = src,
                TagsCsv = string.IsNullOrWhiteSpace(req.TagsCsv) ? null : req.TagsCsv.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Set<TicketEntity>().Add(t);
            await _db.SaveChangesAsync(ct);
            return t.Id;
        }

        public async Task UpdateAsync(Guid id, UpdateTicketRequest req, CancellationToken ct = default)
        {
            var t = await _db.Set<TicketEntity>().FirstOrDefaultAsync(x => x.Id == id, ct)
                    ?? throw new InvalidOperationException("Ticket not found.");

            if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<TicketStatus>(req.Status, true, out var st))
            {
                t.Status = st;
                if (st is TicketStatus.Resolved or TicketStatus.Closed) t.ClosedAt = DateTime.UtcNow;
            }
            if (!string.IsNullOrWhiteSpace(req.Priority) && Enum.TryParse<TicketPriority>(req.Priority, true, out var pr))
                t.Priority = pr;

            if (req.AssignedToUserId.HasValue) t.AssignedToUserId = req.AssignedToUserId;
            if (req.TagsCsv != null) t.TagsCsv = string.IsNullOrWhiteSpace(req.TagsCsv) ? null : req.TagsCsv.Trim();
            if (!string.IsNullOrWhiteSpace(req.Subject)) t.Subject = req.Subject.Trim();
            if (!string.IsNullOrWhiteSpace(req.Body)) t.Body = req.Body.Trim();

            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task AddCommentAsync(Guid id, AddTicketCommentRequest req, CancellationToken ct = default)
        {
            var exists = await _db.Set<TicketEntity>().AnyAsync(x => x.Id == id, ct);
            if (!exists) throw new InvalidOperationException("Ticket not found.");

            var c = new TicketCommentEntity
            {
                TicketId = id,
                UserId = req.UserId,
                Body = (req.Body ?? "").Trim(),
                IsPrivate = req.IsPrivate,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<TicketCommentEntity>().Add(c);

            var t = await _db.Set<TicketEntity>().FirstAsync(x => x.Id == id, ct);
            t.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var t = await _db.Set<TicketEntity>()
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t == null) return;

            _db.Set<TicketCommentEntity>().RemoveRange(t.Comments);
            _db.Set<TicketEntity>().Remove(t);
            await _db.SaveChangesAsync(ct);
        }
    }
}
