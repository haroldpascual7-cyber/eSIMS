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

    public class RepeatIncidentAlertItem
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeSection { get; set; } = string.Empty;
        public int TotalIncidents { get; set; }
        public string AlertLevel { get; set; } = "Review Needed"; // "High Alert (3+)", "Review Needed (2)"
        public string TriggeredPattern { get; set; } = string.Empty;
        public string PrimaryCategory { get; set; } = string.Empty;
        public string LastIncidentDate { get; set; } = string.Empty;
        public int LatestIncidentId { get; set; }
        public int? EnrolledStudentId { get; set; }
    }

    public class RelatedIncidentHistoryItem
    {
        public int IncidentId { get; set; }
        public string IncidentNumber { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Risk { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsSelectedForFollowUp { get; set; }
    }
}