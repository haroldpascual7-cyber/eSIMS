using System;
using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class ProbationRecord : EntityBase
    {
        public string RecordNumber { get; set; } = string.Empty;
        public int IncidentId { get; set; }
        public Incident? Incident { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(14);
        public string Reason { get; set; } = string.Empty;
        public string ExpectedBehaviorConditions { get; set; } = string.Empty;
        public int? AssignedStaffId { get; set; }
        public Staff? AssignedStaff { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string ExtensionReason { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public string OutcomeSummary { get; set; } = string.Empty;
        public ICollection<ProbationProgressNote> ProgressNotes { get; set; } = new List<ProbationProgressNote>();

        public int TotalDays => Math.Max(1, (EndDate.Date - StartDate.Date).Days + 1);
        public string DateRangeText => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
        public bool IsOpen => Status == "Active" || Status == "Extended";
    }
}
