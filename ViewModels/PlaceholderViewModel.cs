using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System.Collections.ObjectModel;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ModuleFeatureItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "⚡";
    }

    public class PlaceholderViewModel : ObservableObject
    {
        public PlaceholderViewModel(string title, string icon, string category, string description, ObservableCollection<ModuleFeatureItem>? features = null)
        {
            Title = title;
            Icon = icon;
            Category = category;
            Description = description;

            Features = features ?? new ObservableCollection<ModuleFeatureItem>
            {
                new ModuleFeatureItem { Icon = "⚙️", Title = "Automated Data Processing", Description = "Connects directly to local SQLite database for instant processing." },
                new ModuleFeatureItem { Icon = "🔒", Title = "Role-Based Security", Description = "Access restricted based on administrator and staff credentials." },
                new ModuleFeatureItem { Icon = "📊", Title = "Real-Time Tracking", Description = "Monitors incident progression and behavioral trajectories." }
            };
        }

        public string Title { get; }
        public string Icon { get; }
        public string Category { get; }
        public string Description { get; }
        public ObservableCollection<ModuleFeatureItem> Features { get; }
    }
}