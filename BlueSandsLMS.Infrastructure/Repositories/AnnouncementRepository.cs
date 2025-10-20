using System;
using System.Linq;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Infrastructure.Repositories
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly BlueSandsLMSDbContext _db;
        public AnnouncementRepository(BlueSandsLMSDbContext db) => _db = db;

        public async Task<Guid> CreateAsync(Guid classroomId, Guid authorUserId, string title, string body)
        {
            var a = new Announcement
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                Title = title,
                Body = body,
                CreatedAt = DateTime.UtcNow,
                // If your entity has CreatedByUserId use it; remove if not present.
                CreatedByUserId = authorUserId
            };
            _db.Announcements.Add(a);
            await _db.SaveChangesAsync();
            return a.Id;
        }

        public async Task UpdateAsync(Guid id, string title, string body)
        {
            var a = await _db.Announcements.FindAsync(id) ?? throw new Exception("Announcement not found");
            a.Title = title;
            a.Body = body;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var a = await _db.Announcements.FindAsync(id) ?? throw new Exception("Announcement not found");
            _db.Announcements.Remove(a);
            await _db.SaveChangesAsync();
        }

        public async Task<Guid?> GetClassroomIdAsync(Guid announcementId)
        {
            return await _db.Announcements
                .Where(x => x.Id == announcementId)
                .Select(x => (Guid?)x.ClassroomId)
                .FirstOrDefaultAsync();
        }
    }
}
