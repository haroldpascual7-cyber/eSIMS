using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class AcknowledgmentLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NoticeId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string IssueDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string DueDate { get; set; } = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd");
        public string Status { get; set; } = "Pending"; // Pending, Overdue, Acknowledged, Non-Compliant
        public string DateAcknowledged { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string AttachedFilePath { get; set; } = string.Empty;

        public bool IsOverdue => Status == "Pending" && DateTime.TryParse(DueDate, out var dt) && dt < DateTime.Today;

        public string StatusBadgeColor => Status switch
        {
            "Acknowledged" => "#059669",
            "Pending" => IsOverdue ? "#DC2626" : "#D97706",
            "Overdue" => "#DC2626",
            "Non-Compliant" => "#DC2626",
            _ => "#64748B"
        };

        public string DisplayStatus => IsOverdue && Status == "Pending" ? "Overdue" : Status;
    }
}