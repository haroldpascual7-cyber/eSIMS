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
    }
}
