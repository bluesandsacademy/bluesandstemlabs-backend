using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;                
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CoreAssignmentType = BlueSandsLMS.Core.Entities.AssignmentType;

namespace BlueSandsLMS.Infrastructure.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly BlueSandsLMSDbContext _db;
        public AssignmentRepository(BlueSandsLMSDbContext db) => _db = db;

        public async Task<Guid> CreateAsync(Guid classroomId, string title, int type, string resourceCode, DateTime? dueAt, Guid creatorUserId)
        {
            var a = new Assignment
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                Title = title,
                Type = (CoreAssignmentType)type,   // <-- explicitly cast to Core enum
                ResourceCode = resourceCode,
                DueAt = dueAt,
                CreatedAt = DateTime.UtcNow,
                // Remove this next line if your entity doesn't have CreatedByUserId
                CreatedByUserId = creatorUserId
            };
            _db.Assignments.Add(a);
            await _db.SaveChangesAsync();
            return a.Id;
        }

        public async Task UpdateAsync(Guid assignmentId, string title, int type, string resourceCode, DateTime? dueAt)
        {
            var a = await _db.Assignments.FindAsync(assignmentId) ?? throw new Exception("Assignment not found");
            a.Title = title;
            a.Type = (CoreAssignmentType)type;   // <-- explicitly cast to Core enum
            a.ResourceCode = resourceCode;
            a.DueAt = dueAt;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid assignmentId)
        {
            var a = await _db.Assignments.FindAsync(assignmentId) ?? throw new Exception("Assignment not found");
            _db.Assignments.Remove(a);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ToGradeItemDto>> GetToGradeAsync(Guid classroomId, int take, int skip)
        {
            var query =
                from s in _db.Submissions
                join a in _db.Assignments on s.AssignmentId equals a.Id
                where a.ClassroomId == classroomId && s.Status == SubmissionStatus.Submitted
                orderby s.SubmittedAt
                select new ToGradeItemDto(
                    s.Id,
                    s.AssignmentId,
                    s.StudentId,
                    s.SubmittedAt,
                    s.Score0to1,
                    (int)s.Status
                );

            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<Guid?> GetClassroomIdAsync(Guid assignmentId)
        {
            return await _db.Assignments
                .Where(a => a.Id == assignmentId)
                .Select(a => (Guid?)a.ClassroomId)
                .FirstOrDefaultAsync();
        }
    }
}
