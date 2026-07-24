using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.DTOs
{
    public record LeaderboardEntryDto(Guid UserId, string FullName, string Email, decimal Score, int Rank);
    public record LeaderboardResponseDto(
        string Scope,
        Guid? ClassId,
        Guid? SchoolId,
        string Metric,
        DateTime GeneratedAtUtc,
        List<LeaderboardEntryDto> Entries
    );
}
