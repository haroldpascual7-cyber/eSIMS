using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class SystemNotificationItem : ObservableObject
    {
        private bool _isRead;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = "Incident"; // Incident, Investigation, Behavior, ParentNotice, Security
        public string Icon { get; set; } = "🔔";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TargetWorkspace { get; set; } = "Incidents"; // Incidents, Investigations, Behavior, ParentNotices, Students, Reports, Settings
        public string? TargetReferenceId { get; set; }
        public string TargetRole { get; set; } = "All"; // All, Administrator, Discipline Officer, Guidance Counselor, Staff
        public string SeverityLevel { get; set; } = "Normal"; // Normal, Warning, High, Urgent

        public bool IsRead
        {
            get => _isRead;
            set => SetProperty(ref _isRead, value);
        }

        public string TimeAgoText
        {
            get
            {
                var span = DateTime.UtcNow - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
                return Timestamp.ToLocalTime().ToString("MMM dd, yyyy");
            }
        }
    }
}
