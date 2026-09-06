using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class StaffRosterModel
    {
        public int StaffId { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AssignmentRole { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }

        public string ToggleLabel => Status == "Active" ? "⛔ Deactivate" : "✅ Activate";
    }
}
