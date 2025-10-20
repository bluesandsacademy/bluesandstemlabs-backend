using System;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Teacher;

namespace BlueSandsLMS.Common.Interfaces.Teacher
{
    public interface ITeacherAnalyticsService
    {
        Task<TeacherOverviewDto>    OverviewAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct);
        Task<TeacherEngagementDto>  EngagementAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct);
        Task<TeacherPerformanceDto> PerformanceAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct);
        Task<TeacherAssignmentsDto> AssignmentsAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct);
        Task<TeacherAttendanceDto>  AttendanceAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct);
    }
}
