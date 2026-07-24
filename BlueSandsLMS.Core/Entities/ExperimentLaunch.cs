using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class ExperimentLaunch
    {
        [Key] public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? ClassroomId { get; set; }

public Guid? PhETSimulationId { get; set; }

        public string Subject { get; set; } = "";
         public string ExperimentName { get; set; } = "";
         public string Mode { get; set; } = "guided";
          public int LastStep { get; set; }  
        [MaxLength(100)] public string ExperimentCode { get; set; } = "";
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
         public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public int DurationSec { get; set; }
        public bool Completed { get; set; }

        public Classroom? Classroom { get; set; }

public PhETSimulation? PhETSimulation { get; set; }
        public User? User { get; set; }
    }
}
