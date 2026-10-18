using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class Investigation : EntityBase
    {
        public int IncidentId { get; set; }
        public Incident? Incident { get; set; }

        public int? InvestigatorId { get; set; }
        public Staff? Investigator { get; set; }

        // Free-text investigator name (replaces ComboBox staff selection)
        public string LeadInvestigatorName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? ClosedDate { get; set; }

        public string InvestigationNotes { get; set; } = string.Empty;
        public string InterviewLogs { get; set; } = string.Empty;
        public string WitnessStatements { get; set; } = string.Empty;
        
        // Findings: Pending, Substantiated, Unsubstantiated, Inconclusive, Dismissed
        public string Findings { get; set; } = "Pending";
        public string FinalSummary { get; set; } = string.Empty;

        // Structured timeline entries stored as JSON
        public string InvestigationTimeline { get; set; } = string.Empty;

        // Flagging missing or conflicting information for follow-up
        public bool IsFollowUpFlagged { get; set; }
        public string FollowUpIssueType { get; set; } = string.Empty;
        public string FollowUpNotes { get; set; } = string.Empty;
        public bool IsFollowUpResolved { get; set; }
    }

    public class InvestigationTimelineEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ActionTitle { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string EntryType { get; set; } = "Note"; // Note, Interview, Witness, Evidence, StatusChange
    }

    public class InvestigationEvidenceItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AddedAt { get; set; } = string.Empty;
    }
}