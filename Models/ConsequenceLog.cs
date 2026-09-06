using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class ConsequenceLog : EntityBase
    {
        public string LogId { get; set; } = string.Empty;
        public string IncidentNumber { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ConsequenceType { get; set; } = "Verbal Warning";
        public string OffenseLevel { get; set; } = "1st Offense";
        public DateTime DateIssued { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);
        public string IssuedBy { get; set; } = "admin";
        public bool ParentNotified { get; set; } = true;
        public bool ParentSigned { get; set; } = false;
        public string Status { get; set; } = "Scheduled"; // Pending, Scheduled, Completed, Overdue
        public string SanctionTerms { get; set; } = string.Empty;

        // Display Helper
        public string StudentNameAndId => $"{StudentName} - {StudentIdNumber}";
    }
}