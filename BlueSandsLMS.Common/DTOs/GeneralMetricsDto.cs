namespace BlueSandsLMS.Common.DTOs;

public record GeneralMetricsDto(
       int TotalPlatformUsers,
       int TotalSchoolsRegistered,
       int TotalVirtualLabExperiments,
       int TotalPayments
   );