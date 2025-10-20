using System;
using System.Threading.Tasks;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IAnnouncementRepository
    {
        Task<Guid> CreateAsync(Guid classroomId, Guid authorUserId, string title, string body);
        Task UpdateAsync(Guid id, string title, string body);
        Task DeleteAsync(Guid id);

        // Guard helper
        Task<Guid?> GetClassroomIdAsync(Guid announcementId);
    }
}
