using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class DisciplinaryAction : EntityBase
    {
        public int IncidentId { get; set; }
        public Incident? Incident { get; set; }

        public int? StudentId { get; set; }
        public Student? Student { get; set; }

        // ActionType: Verbal Warning, Written Reprimand, Parent Conference, Counseling Referral, Behavioral Contract, School Service, Suspension, Probation
        public string ActionType { get; set; } = "Verbal Warning";
        public string ActionDetails { get; set; } = string.Empty;

        public DateTime ScheduledDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }

        public int? AssignedStaffId { get; set; }
        public Staff? AssignedStaff { get; set; }

        // Status: Pending, In Progress, Completed, Waived
        public string Status { get; set; } = "Pending";
        public string FollowUpNotes { get; set; } = string.Empty;
    }
}