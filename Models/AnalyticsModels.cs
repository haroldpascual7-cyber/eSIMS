using System;
using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class ConductAnalyticsSummary
    {
        public double AverageConductScore { get; set; } = 88.0;
        public string HighestRiskSection { get; set; } = "Grade 10 - St. Thomas";
        public int ActiveInterventionsCount { get; set; } = 8;
        public double EscalationRatePercent { get; set; } = 12.4;
        public bool EscalationRateIncreased { get; set; } = true;
        public string EscalationRateText => EscalationRateIncreased ? $"+{EscalationRatePercent:F1}% ↗" : $"-{EscalationRatePercent:F1}% ↘";
    }

    public class CategoryFrequencyModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int IncidentCount { get; set; }
    }

    public class MonthlyTrajectoryModel
    {
        public string MonthLabel { get; set; } = string.Empty;
        public double AverageScore { get; set; }
    }

    public class WatchlistStudentModel
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeSection { get; set; } = string.Empty;
        public int CurrentScore { get; set; }
        public int ThirtyDayDrop { get; set; }
        public string ThirtyDayDropText => $"-{ThirtyDayDrop} pts";
        public string StandingLabel { get; set; } = "AT RISK";
        public string StandingColor => CurrentScore < 75 ? "#DC2626" : "#EA580C";
    }
}