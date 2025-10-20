// BlueSandsLMS.Common.Interfaces.Teacher/ITeacherCommAnalyticsService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Teacher;

namespace BlueSandsLMS.Common.Interfaces.Teacher
{
    public interface ITeacherCommAnalyticsService
    {
        Task<TeacherCommOverviewDto>  CommOverviewAsync(Guid teacherId, Guid? classroomId, DateTime from, DateTime to, CancellationToken ct);
        Task<TeacherForumOverviewDto> ForumOverviewAsync(Guid teacherId, Guid? classroomId, DateTime from, DateTime to, CancellationToken ct);
    }
}
