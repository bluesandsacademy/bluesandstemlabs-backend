using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IParentLinkRepository
    {
        Task AddAsync(Guid studentId, string parentEmail, bool isPrimary);
        Task RemoveAsync(Guid parentLinkId);
        Task<List<ParentLinkDto>> GetByStudentAsync(Guid studentId);
    }
}
