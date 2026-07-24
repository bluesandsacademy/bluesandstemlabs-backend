using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Infrastructure.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly BlueSandsLMSDbContext _db;
        public SubmissionRepository(BlueSandsLMSDbContext db) => _db = db;

        public async Task<Guid?> GetClassroomIdByAssignmentAsync(Guid assignmentId)
        {
            return await _db.Assignments
                .Where(a => a.Id == assignmentId)
                .Select(a => (Guid?)a.ClassroomId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsStudentEnrolledAsync(Guid assignmentId, Guid studentId)
        {
            var classId = await GetClassroomIdByAssignmentAsync(assignmentId);
            if (classId is null) return false;

            return await _db.Enrollments.AnyAsync(e =>
                e.ClassroomId == classId.Value &&
                e.UserId == studentId &&
                e.RoleInClass == ClassRole.Student);
        }

        public async Task<bool> IsTeacherOfAssignmentAsync(Guid assignmentId, Guid teacherId)
        {
            var classId = await GetClassroomIdByAssignmentAsync(assignmentId);
            if (classId is null) return false;

            return await _db.Enrollments.AnyAsync(e =>
                e.ClassroomId == classId.Value &&
                e.UserId == teacherId &&
                e.RoleInClass == ClassRole.Teacher);
        }

        public async Task<Guid> SubmitAsync(Guid assignmentId, Guid studentId, SubmitWorkDto dto)
        {

            var enrolled = await IsStudentEnrolledAsync(assignmentId, studentId);
            if (!enrolled) throw new Exception("Not enrolled in this class.");


            var existing = await _db.Submissions.FirstOrDefaultAsync(s =>
                s.AssignmentId == assignmentId && s.StudentId == studentId);

            if (existing == null)
            {
                existing = new Submission
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = assignmentId,
                    StudentId = studentId
                };
                _db.Submissions.Add(existing);
            }

            existing.AttachmentUrl = dto.AttachmentUrl;
            existing.AttachmentName = dto.AttachmentName;
            existing.AttachmentSizeBytes = dto.AttachmentSizeBytes;
            existing.AttachmentContentType = dto.AttachmentContentType;


            existing.Status = SubmissionStatus.Submitted;
            existing.SubmittedAt = DateTime.UtcNow;


            existing.GraderUserId = null;
            existing.GradedAt = null;
            existing.Score0to1 = null;
            existing.Feedback = null;

            await _db.SaveChangesAsync();
            return existing.Id;
        }

        public async Task ResubmitAsync(Guid submissionId, Guid studentId, ResubmitWorkDto dto)
        {
            var s = await _db.Submissions.FindAsync(submissionId) ?? throw new Exception("Submission not found");
            if (s.StudentId != studentId) throw new Exception("Cannot resubmit another student's work.");

            s.AttachmentUrl = dto.AttachmentUrl;
            s.AttachmentName = dto.AttachmentName;
            s.AttachmentSizeBytes = dto.AttachmentSizeBytes;
            s.AttachmentContentType = dto.AttachmentContentType;

            s.Status = SubmissionStatus.Submitted;
            s.SubmittedAt = DateTime.UtcNow;


            s.GraderUserId = null;
            s.GradedAt = null;
            s.Score0to1 = null;
            s.Feedback = null;

            await _db.SaveChangesAsync();
        }

        public async Task GradeAsync(Guid submissionId, Guid teacherId, decimal score0to1, string? feedback)
        {
            var s = await _db.Submissions.FindAsync(submissionId) ?? throw new Exception("Submission not found");


            var ok = await IsTeacherOfAssignmentAsync(s.AssignmentId, teacherId);
            if (!ok) throw new Exception("Not permitted to grade this submission.");

            if (score0to1 < 0m || score0to1 > 1m) throw new Exception("Score must be in range [0..1].");

            s.Score0to1 = score0to1;
            s.Feedback = feedback;
            s.GraderUserId = teacherId;
            s.GradedAt = DateTime.UtcNow;
            s.Status = SubmissionStatus.Graded;

            await _db.SaveChangesAsync();
        }

        public async Task<List<SubmissionSummaryDto>> ListByAssignmentAsync(Guid assignmentId, int skip, int take)
        {
            var rows = await (
                from s in _db.Submissions
                join u in _db.Users on s.StudentId equals u.Id
                where s.AssignmentId == assignmentId
                orderby s.SubmittedAt descending, u.FullName
                select new SubmissionSummaryDto(
                    s.Id, s.AssignmentId, s.StudentId,
                    u.FullName ?? "Student", u.Email ?? "",
                    (int)s.Status, s.Score0to1, s.SubmittedAt, s.GradedAt
                )
            ).Skip(skip).Take(take).ToListAsync();

            return rows;
        }

        public async Task<SubmissionDetailDto?> GetMineAsync(Guid assignmentId, Guid studentId)
        {
            return await _db.Submissions
                .Where(s => s.AssignmentId == assignmentId && s.StudentId == studentId)
                .Select(s => new SubmissionDetailDto(
                    s.Id, s.AssignmentId, s.StudentId, (int)s.Status,
                    s.Score0to1, s.SubmittedAt, s.GradedAt,
                    s.Feedback, s.GraderUserId,
                    s.AttachmentUrl, s.AttachmentName, s.AttachmentSizeBytes, s.AttachmentContentType
                ))
                .FirstOrDefaultAsync();
        }
    }
}
