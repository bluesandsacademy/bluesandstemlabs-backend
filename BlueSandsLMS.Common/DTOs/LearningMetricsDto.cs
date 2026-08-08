namespace BlueSandsLMS.Common.DTOs;

public record LearningMetricsDto(
        int TotalStemCourses,
        long TotalLabPracticeTimeMinutes,
        double TotalQuizScores,
        long TotalExperimentAttempts,
        int TotalIlsCreated
    );