using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class InterventionProgressLog : EntityBase
    {
        public string LogId { get; set; } = string.Empty;
        public int RestorativePlanId { get; set; }
        public RestorativePlan? RestorativePlan { get; set; }
        public int LoggedUnits { get; set; } = 1; // Hours or sessions logged
        public DateTime LogDate { get; set; } = DateTime.Now;
        public string StaffName { get; set; } = "Guidance Office";
        public string Notes { get; set; } = string.Empty;
    }
}