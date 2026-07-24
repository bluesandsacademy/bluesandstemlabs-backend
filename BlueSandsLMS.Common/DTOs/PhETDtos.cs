using System;

namespace BlueSandsLMS.Common.DTOs
{

    public class PhETSimulationDto
    {

        public Guid Id { get; set; }
        public string Title { get; set; }


        public string? Type { get; set; }


        public int? NumberOfScreens { get; set; }


        public string? ScreenNames { get; set; }


        public string? SimPage { get; set; }


        public string? SimString { get; set; }


        public string? TeacherTipsDoc { get; set; }


        public string? PdfUrl { get; set; }


        public bool Physics { get; set; }
        public bool MathStatistics { get; set; }
        public bool Chemistry { get; set; }
        public bool EarthSpace { get; set; }
        public bool Biology { get; set; }


        public string? LowGradeLevel { get; set; }


        public string? HighGradeLevel { get; set; }


        public string? MainTopics { get; set; }


        public string? Keywords { get; set; }


        public string? Description { get; set; }


        public string? SampleLearningGoals { get; set; }


        public string? Translations { get; set; }


        public string? Published { get; set; }


        public string? RunnableResource { get; set; }


        public string? CheerpJRunnable { get; set; }


        public string? Filename { get; set; }
    }

    public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
    
    public record ImportResult(int Imported, int Updated, int Skipped, IReadOnlyList<string> Errors);
    
    public record SeedResult(int Count, string Message);
}