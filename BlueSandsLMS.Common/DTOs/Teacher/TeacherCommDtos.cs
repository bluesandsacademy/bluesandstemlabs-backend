// BlueSandsLMS.Common/Teacher/TeacherCommDtos.cs
using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.Teacher
{
    public sealed class TeacherCommOverviewDto
    {
        public int MessagesSent { get; set; }
        public int MessagesReceived { get; set; }
        public int UnreadReceived { get; set; }
        public double AvgResponseTimeMinutes { get; set; }
        public LabeledCount[] TopThreads { get; set; } = Array.Empty<LabeledCount>();
    }

    public sealed class TeacherForumOverviewDto
    {
        public int TopicsCreated { get; set; }
        public int PostsMade { get; set; }
        public int UniqueParticipants { get; set; }
        public DayPoint[] ActivityTrend { get; set; } = Array.Empty<DayPoint>();
        public LabeledCount[] BusiestTopics { get; set; } = Array.Empty<LabeledCount>();
    }
}
