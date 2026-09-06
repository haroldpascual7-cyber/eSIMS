using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class ExecutiveReportFlags
    {
        public bool IsIncidentVolumeIncluded { get; set; } = true;
        public bool IsHeatmapIncluded { get; set; } = true;
        public bool IsRecidivismIncluded { get; set; } = true;
        public bool IsComplianceIncluded { get; set; } = true;
        public bool IsSavePdfLocal { get; set; } = true;
        public bool IsEmailToPrincipal { get; set; } = false;
    }

    public class ExecutiveReportData
    {
        public string PeriodType { get; set; } = "Monthly";
        public string TimeframeText { get; set; } = "August 2026";
        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        // Incident Metrics
        public int TotalIncidents { get; set; } = 42;
        public string TopInfractionCategory { get; set; } = "Bullying & Harassment";
        public int ActiveSuspensionsCount { get; set; } = 3;

        // Heatmap Metrics
        public string TopDangerLocation { get; set; } = "Cafeteria";
        public int TopDangerLocationIncidents { get; set; } = 18;

        // Recidivism Metrics
        public double RecidivismRatePercent { get; set; } = 22.0;
        public int ChronicOffendersCount { get; set; } = 3;

        // Parent Compliance Metrics
        public int TotalNoticesDispatched { get; set; } = 38;
        public int AcknowledgedNoticesCount { get; set; } = 31;
        public double ComplianceRatePercent { get; set; } = 81.6;
    }
}