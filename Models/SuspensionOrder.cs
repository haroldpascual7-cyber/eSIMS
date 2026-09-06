using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class SuspensionOrder : EntityBase
    {
        public string OrderId { get; set; } = string.Empty;
        public string IncidentNumber { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string SuspensionType { get; set; } = "In-School Suspension (ISS)"; // ISS, OSS, Preventative Exclusion
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(5);
        public int TotalDays { get; set; } = 5;
        public DateTime ReentryDate { get; set; } = DateTime.Now.AddDays(6);
        public string IssuedBy { get; set; } = "Prefect of Discipline";
        public string Terms { get; set; } = string.Empty;

        // Re-entry Checklist Required Flags
        public bool ParentConferenceRequired { get; set; } = true;
        public bool AcademicPacketRequired { get; set; } = true;
        public bool CounselorIntakeRequired { get; set; } = true;

        // Re-entry Checklist Passed Flags
        public bool ParentConferencePassed { get; set; } = false;
        public bool AcademicPacketPassed { get; set; } = false;
        public bool CounselorIntakePassed { get; set; } = false;

        public bool IsCleared { get; set; } = false;
        public string Status { get; set; } = "Active Ban"; // Active Ban, Pending Review, Readmitted
        public string ClearanceNotes { get; set; } = string.Empty;

        // Display Helpers
        public string StudentNameAndId => $"{StudentName} - {StudentIdNumber}";
        public string DateSpanText => $"{StartDate:yyyy-MM-dd} → {EndDate:yyyy-MM-dd}";
        
        public int RequiredChecklistCount => (ParentConferenceRequired ? 1 : 0) + (AcademicPacketRequired ? 1 : 0) + (CounselorIntakeRequired ? 1 : 0);
        public int CompletedChecklistCount => (ParentConferencePassed ? 1 : 0) + (AcademicPacketPassed ? 1 : 0) + (CounselorIntakePassed ? 1 : 0);
        public string ChecklistStatusText => $"{CompletedChecklistCount}/{RequiredChecklistCount} Passed";
    }
}