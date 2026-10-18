using System.Collections.Generic;
using System.Windows.Media;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class HeatmapNodeModel
    {
        public string LocationName { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public int IncidentCount { get; set; }
        public string PrimaryInfraction { get; set; } = "General Misconduct";
        public List<Incident> Incidents { get; set; } = new();
        
        public double Radius { get; set; } = 40;
        public Brush RiskColor { get; set; } = Brushes.Orange;
        public string RiskLevelText { get; set; } = "Moderate";
        public string RiskBadgeColor { get; set; } = "#D97706";

        public double CanvasLeft => X - (Radius / 2);
        public double CanvasTop  => Y - (Radius / 2);
        public double BadgeLeft  => X - 62;
        public double BadgeTop   => Y - 18;

        public Brush NodeBorderBrush { get; set; } = Brushes.Transparent;
        public Brush NodeTextBrush { get; set; } = Brushes.Gray;

        public string CountDisplayText => IncidentCount > 0
            ? $"⚠️ {IncidentCount} incident{(IncidentCount == 1 ? "" : "s")}"
            : "✓ 0 incidents";
    }
}