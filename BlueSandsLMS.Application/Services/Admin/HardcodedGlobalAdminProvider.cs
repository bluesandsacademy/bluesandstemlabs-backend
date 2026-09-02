using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;

namespace BlueSandsLMS.Application.Services.Admin
{
    public  class HardcodedGlobalAdminProvider : IHardcodedGlobalAdminProvider
    {
        public Task<PromptTotalsDto> GetPromptTotalsAsync(CancellationToken ct = default)
        {
            var dto = new PromptTotalsDto(
                TotalPlatformUsers: 12610,
                TotalSchoolsRegistered: 40,
                TotalStemCourses: 176,
                TotalPayments: 32761250L,
                TotalLabPractice: 12583,
                TotalExperimentAttempts: 539,
                TotalQuizAttempts: 375,
                TotalQuizScorePercent: 84.0,
                TotalILScreated: 900,
                SubscribedUsers: 4352, // not specified in prompt.md; set to 0
                ActiveUsers: 10000,
                MaleUsers: 7566,
                ActiveSubscriptions: 1000,
                PaymentRecorded: 32761250L,
                FemaleUsers: 5044,
                OfflineUsers: 71,
                GeneratedAtUtc: DateTime.UtcNow
            );

            return Task.FromResult(dto);
        }
    }
}