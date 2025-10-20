using System;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Student;

namespace BlueSandsLMS.Common.Interfaces.Student
{
    public interface IStudentActionsService
    {
        Task<StartExperimentResponse> StartExperimentAsync(
            Guid userId,
            StartExperimentRequest req,
            CancellationToken ct = default);

        Task SaveExperimentProgressAsync(
            Guid userId,
            Guid launchId,
            SaveExperimentProgressRequest req,
            CancellationToken ct = default);

        Task CompleteExperimentAsync(
            Guid userId,
            Guid launchId,
            CompleteExperimentRequest req,
            CancellationToken ct = default);

        Task<SubmitQuizResponse> SubmitQuizAsync(
            Guid userId,
            SubmitQuizRequest req,
            CancellationToken ct = default);
    }
}
