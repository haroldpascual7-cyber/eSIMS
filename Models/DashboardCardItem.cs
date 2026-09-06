using System.Windows.Input;
using System.Windows.Media;

namespace SmartSchoolIncidentManagementSystem.Models
{
    /// <summary>
    /// Represents a dynamic dashboard card tile that auto-arranges based on RBAC permissions.
    /// </summary>
    public class DashboardCardItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public Brush BackgroundBrush { get; set; } = Brushes.SlateGray;
        public ICommand? Command { get; set; }
    }
}
