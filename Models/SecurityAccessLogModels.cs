using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class AccessSecurityLogModel
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string FormattedTimestamp => Timestamp.ToString("MM/dd/yyyy hh:mm tt");
        public string AttemptedUsername { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // Login, Logout, Failed Login, Role Change, Account Lockout
        public string Status { get; set; } = "Success"; // Success, Failed, Blocked
        public string Details { get; set; } = string.Empty;

        public bool IsLockedAccount => EventType.Equals("Account Lockout", StringComparison.OrdinalIgnoreCase) || Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase);

        public string StatusColor => Status switch
        {
            "Success" => "#059669",
            "Failed"  => "#DC2626",
            "Blocked" => "#DC2626",
            _         => "#475569"
        };
    }
}