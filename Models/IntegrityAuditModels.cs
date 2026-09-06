using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class DiagnosticResultModel
    {
        public string TestName { get; set; } = string.Empty;
        public string TargetComponent { get; set; } = string.Empty;
        public string Status { get; set; } = "PASSED"; // PASSED, FAILED, WARNING
        public string Details { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; } = DateTime.Now;

        public string StatusColor => Status switch
        {
            "PASSED"  => "#059669",
            "FAILED"  => "#DC2626",
            "WARNING" => "#D97706",
            _         => "#64748B"
        };
    }
}