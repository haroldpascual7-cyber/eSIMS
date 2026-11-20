using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class MainViewModel
    {
        private bool _isSidebarCollapsed;

        /// <summary>
        /// Gets or sets whether the left navigation sidebar is in collapsed (icons only) mode.
        /// </summary>
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                if (SetProperty(ref _isSidebarCollapsed, value))
                {
                    OnPropertyChanged(nameof(SidebarWidth));
                }
            }
        }

        /// <summary>
        /// Width in pixels: 60px when collapsed, 230px when expanded.
        /// </summary>
        public double SidebarWidth => _isSidebarCollapsed ? 60.0 : 230.0;

        /// <summary>
        /// Detailed tooltip text for user profile badge in collapsed mode.
        /// </summary>
        public string UserTooltip => $"{CurrentUserName} ({CurrentUserRole}) - Online";

        private ICommand? _toggleSidebarCommand;

        /// <summary>
        /// Toggles the sidebar between Collapsed and Expanded states without reloading or renavigating.
        /// </summary>
        public ICommand ToggleSidebarCommand => _toggleSidebarCommand ??= new RelayCommand(() =>
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        });

        /// <summary>
        /// Indicates if the application is currently in dark mode.
        /// </summary>
        public bool IsDarkMode => _themeService.CurrentTheme == Services.AppTheme.Dark;

        /// <summary>
        /// Dynamic icon for theme toggle button (shows opposite target theme or current mode).
        /// </summary>
        public string ThemeToggleIcon => IsDarkMode ? "☀️" : "🌙";

        /// <summary>
        /// Dynamic tooltip for theme toggle button.
        /// </summary>
        public string ThemeToggleTooltip => IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";

        /// <summary>
        /// Initializes theme change monitoring for the sidebar.
        /// </summary>
        private void InitializeSidebarSupport()
        {
            _themeService.ThemeChanged += OnThemeChangedNotify;
        }

        private void OnThemeChangedNotify(Services.AppTheme newTheme)
        {
            OnPropertyChanged(nameof(IsDarkMode));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleTooltip));
        }
    }
}
