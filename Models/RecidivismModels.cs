using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class RecidivismSummaryModel
    {
        public double RecidivismRate { get; set; } = 14.2;
        public string MostEffectiveIntervention { get; set; } = "Restorative Counseling (88% Success)";
        public int SingleOffenderCount { get; set; } = 32;
        public int RepeatOffenderCount { get; set; } = 6;
        public int ChronicOffenderCount { get; set; } = 3;
    }

    public class ChronicOffenderWatchItem
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeSection { get; set; } = "Grade 10 - St. Thomas";
        public int TotalIncidents { get; set; }
        public string MostCommonOffense { get; set; } = string.Empty;
        public string ActiveIapStatus { get; set; } = "Active Plan";
        public string LastIncidentDate { get; set; } = string.Empty;
    }

    public class InterventionSuccessMetric
    {
        public string InterventionType { get; set; } = string.Empty;
        public double SuccessRatePercent { get; set; }
        public int TotalCaseCount { get; set; }
    }
}