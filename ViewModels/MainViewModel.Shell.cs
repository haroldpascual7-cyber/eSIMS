using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    /// <summary>
    /// Shell-level navigation surface added by the UI/UX refactor.
    ///
    /// "Home" is the operational dashboard (the page users actually work from) and
    /// "Kiosk board" is the full-screen wall display, demoted from being the landing
    /// page to an explicit action. Both are reachable from the title bar.
    /// </summary>
    public partial class MainViewModel
    {
        private ICommand? _navigateHomeCommand;
        private ICommand? _navigateKioskCommand;

        /// <summary>Home = operational dashboard, with the navigation pane visible.</summary>
        public ICommand NavigateHomeCommand => _navigateHomeCommand ??= new RelayCommand(ShowOperationalDashboard);

        /// <summary>Full-screen kiosk board used for wall displays and presentations.</summary>
        public ICommand NavigateKioskCommand => _navigateKioskCommand ??= new RelayCommand(ShowDashboard);

        /// <summary>True while the full-screen kiosk board is on screen (chrome is hidden).</summary>
        public bool IsKioskActive => ActiveSection == "Dashboard";

        /// <summary>True when the left navigation sidebar should be shown (authenticated, non-kiosk views).</summary>
        public bool ShowSidebar => IsAuthenticated && !IsKioskActive;

        /// <summary>True while the operational dashboard is on screen.</summary>
        public bool IsHomeActive => ActiveSection == "Dashboard Overview";

        /// <summary>Short, plain label for the title-bar breadcrumb.</summary>
        public string SectionBreadcrumb => ActiveSection switch
        {
            "Dashboard" => "Kiosk board",
            "Dashboard Overview" => "Dashboard",
            "DisciplinaryActions" => "Disciplinary actions",
            "Behavior" => "Behavior & intervention",
            "ParentNotices" => "Parent notices",
            "ReportsAnalytics" => "Reports",
            "Settings" => "Administration",
            "Incidents" => "Incidents",
            "Investigations" => "Investigations",
            "Students" => "Students",
            _ => ActiveSection
        };

        /// <summary>Initials shown in the navigation pane avatar.</summary>
        public string UserDisplayName =>
            string.IsNullOrWhiteSpace(CurrentUserName) ? "Signed out" : CurrentUserName;
    }
}
