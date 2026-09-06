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
    }
}