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
        
        public double Radius { get; set; } = 40;
        public Brush RiskColor { get; set; } = Brushes.Orange;
        public string RiskLevelText { get; set; } = "Moderate";
        public string RiskBadgeColor { get; set; } = "#D97706";

        public double CanvasLeft => X - (Radius / 2);
        public double CanvasTop  => Y - (Radius / 2);
    }
}