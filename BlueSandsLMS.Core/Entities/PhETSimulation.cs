using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class PhETSimulation
    {
        [Key]
        public Guid Id { get; set; }
        

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;
        
        [MaxLength(50)]
        public string? Type { get; set; }
        
        public int? NumberOfScreens { get; set; }
        
        [MaxLength(500)]
        public string? ScreenNames { get; set; }
        

        [MaxLength(1000)]
        public string? SimPage { get; set; }
        
        [MaxLength(200)]
        public string? SimString { get; set; }
        
        [MaxLength(1000)]
        public string? TeacherTipsDoc { get; set; }
        
        [MaxLength(1000)]
        public string? PdfUrl { get; set; }
        
        [MaxLength(1000)]
        public string? RunnableResource { get; set; }
        
        [MaxLength(1000)]
        public string? CheerpJRunnable { get; set; }
        
        [MaxLength(200)]
        public string? Filename { get; set; }
        

        public bool Physics { get; set; }
        public bool MathStatistics { get; set; }
        public bool Chemistry { get; set; }
        public bool EarthSpace { get; set; }
        public bool Biology { get; set; }
        


[MaxLength(1000)]
public string? SimulationUrl { get; set; }

[MaxLength(1000)]
public string? ThumbnailUrl { get; set; }

[MaxLength(200)]
public string? Topic { get; set; }

[MaxLength(50)]
public string? GradeLevel { get; set; }

[MaxLength(2000)]
public string? Standards { get; set; }

[MaxLength(3000)]
public string? LearningGoals { get; set; }



        [MaxLength(50)]
        public string? LowGradeLevel { get; set; }
        
        [MaxLength(50)]
        public string? HighGradeLevel { get; set; }
        

        [MaxLength(1000)]
        public string? MainTopics { get; set; }
        
        [MaxLength(2000)]
        public string? Keywords { get; set; }
        
        [MaxLength(3000)]
        public string? Description { get; set; }
        
        [MaxLength(3000)]
        public string? SampleLearningGoals { get; set; }
        
        [MaxLength(500)]
        public string? Translations { get; set; }
        
        [MaxLength(50)]
        public string? Published { get; set; }
        

        public bool IsActive { get; set; } = true;

        public bool IsFree { get; set; } = false;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
        

        public ICollection<ExperimentLaunch> Launches { get; set; } = new List<ExperimentLaunch>();
    }
}