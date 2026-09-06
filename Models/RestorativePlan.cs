using System;
using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class RestorativePlan : EntityBase
    {
        public string PlanId { get; set; } = string.Empty;
        public string IncidentNumber { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string PlanType { get; set; } = "Community Service"; // Community Service, Individual Counseling, Peer Mediation, Behavioral Contract, Anger Management Workshop
        public string AssignedStaff { get; set; } = "Guidance Counselor";
        public int TargetMetric { get; set; } = 10; // Total required hours or sessions
        public int CompletedMetric { get; set; } = 0; // Current logged hours or sessions
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime DeadlineDate { get; set; } = DateTime.Now.AddDays(14);
        public string Objectives { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Pending Sign-off, Completed, Overdue
        public bool IsCleared { get; set; } = false;

        public List<InterventionProgressLog> ProgressLogs { get; set; } = new();

        // Display Helpers
        public string StudentNameAndId => $"{StudentName} - {StudentIdNumber}";
        public string MetricUnitLabel => PlanType.Contains("Counseling") || PlanType.Contains("Mediation") || PlanType.Contains("Workshop") ? "Sessions" : "Hours";
        public string ProgressText => $"{CompletedMetric} / {TargetMetric} {MetricUnitLabel}";
        public double ProgressPercentage => TargetMetric > 0 ? Math.Min(100, Math.Round((double)CompletedMetric / TargetMetric * 100, 1)) : 0;
    }
}