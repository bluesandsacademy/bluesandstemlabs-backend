using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Infrastructure.Repositories
{
    public class ParentLinkRepository : IParentLinkRepository
    {
        private readonly BlueSandsLMSDbContext _db;
        public ParentLinkRepository(BlueSandsLMSDbContext db) => _db = db;

        public async Task AddAsync(Guid studentId, string parentEmail, bool isPrimary)
        {
            if (isPrimary)
            {
                var primaries = await _db.ParentLinks
                    .Where(p => p.StudentId == studentId && p.IsPrimary)
                    .ToListAsync();
                foreach (var p in primaries) p.IsPrimary = false;
            }

            _db.ParentLinks.Add(new ParentLink
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                ParentEmail = parentEmail,
                IsPrimary = isPrimary,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task RemoveAsync(Guid parentLinkId)
        {
            var p = await _db.ParentLinks.FindAsync(parentLinkId) ?? throw new Exception("Parent link not found");
            _db.ParentLinks.Remove(p);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ParentLinkDto>> GetByStudentAsync(Guid studentId)
        {
            return await _db.ParentLinks
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.IsPrimary).ThenBy(p => p.ParentEmail)
                .Select(p => new ParentLinkDto(p.Id, p.ParentEmail, p.IsPrimary, p.CreatedAt))
                .ToListAsync();
        }
    }
}
