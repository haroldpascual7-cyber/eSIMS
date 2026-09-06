using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class Incident : EntityBase
    {
        public string IncidentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "General Misconduct";
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; } = DateTime.Now;

        // Manually entered Student and Staff names
        public string PrimaryInvolvedStudent { get; set; } = string.Empty;
        public string ReportingStaffTeacher { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;

        // Foreign keys and relationships (optional / legacy)
        public int? StudentId { get; set; }
        public Student? Student { get; set; }

        public int? StaffId { get; set; }
        public Staff? Reporter { get; set; }

        public string InvolvedPersons { get; set; } = string.Empty;
        public string Witnesses { get; set; } = string.Empty;

        public IncidentStatus Status { get; set; } = IncidentStatus.Reported;
        public IncidentSeverity Severity { get; set; } = IncidentSeverity.Low;
        public RiskLevel Risk { get; set; } = RiskLevel.Low;

        // Rule-Based Engine Outputs
        public string RecommendedAction { get; set; } = string.Empty;
        public bool IsRepeatIncident { get; set; } = false;
        public int RepeatIncidentCount { get; set; } = 0;

        public string ActionTaken { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Helper for unified display across lists
        public string StudentDisplayName => !string.IsNullOrWhiteSpace(PrimaryInvolvedStudent)
            ? PrimaryInvolvedStudent
            : (Student?.FullName ?? string.Empty);

        public string ReporterDisplayName => !string.IsNullOrWhiteSpace(ReportingStaffTeacher)
            ? ReportingStaffTeacher
            : (Reporter?.FullName ?? string.Empty);
    }
}