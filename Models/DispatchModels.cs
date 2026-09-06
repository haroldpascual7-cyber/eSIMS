using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class DispatchLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudentId { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string ContactDestination { get; set; } = string.Empty;
        public string Channel { get; set; } = "SMS"; // SMS, Email, Both
        public string MessageBody { get; set; } = string.Empty;
        public string Status { get; set; } = "Sent"; // Sent, Pending, Failed
        public DateTime SentAt { get; set; } = DateTime.Now;

        public string FormattedDate => SentAt.ToString("yyyy-MM-dd HH:mm:ss");
        public string StatusColor => Status switch
        {
            "Sent" => "#059669",
            "Pending" => "#D97706",
            "Failed" => "#DC2626",
            _ => "#64748B"
        };
    }

    public class QuickTemplateItem
    {
        public string Title { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
    }
}