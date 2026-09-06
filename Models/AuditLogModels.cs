using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class AuditLogModel
    {
        public long LogId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string FormattedTimestamp => Timestamp.ToString("MM/dd/yyyy hh:mm tt");
        public string UserId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // LOGIN, LOGOUT, CREATE, UPDATE, DELETE, EXPORT
        public string ModuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string ActionTypeColor => ActionType switch
        {
            "DELETE" => "#DC2626",
            "CREATE" => "#059669",
            "LOGIN"  => "#059669",
            "UPDATE" => "#2563EB",
            "EXPORT" => "#2563EB",
            "LOGOUT" => "#D97706",
            _        => "#475569"
        };
    }
}