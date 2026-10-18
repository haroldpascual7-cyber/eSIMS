using SmartSchoolIncidentManagementSystem.Services;
using System;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class MainViewModel
    {
        public string SchoolName => _settingsService.SchoolName;
        public string SchoolAddress => _settingsService.SchoolAddress;
        public string SystemTitle => _settingsService.SystemTitle;
        public string SystemAcronym => _settingsService.SystemAcronym;
        public object? DashboardLogoSource => _settingsService.DashboardLogoSource;

        public string SchoolNameFirstLine
        {
            get
            {
                var name = SchoolName;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 1) return name.ToUpperInvariant();
                int mid = (parts.Length + 1) / 2;
                return string.Join(" ", parts[..mid]).ToUpperInvariant();
            }
        }

        public string SchoolNameSecondLine
        {
            get
            {
                var name = SchoolName;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 1) return string.Empty;
                int mid = (parts.Length + 1) / 2;
                return string.Join(" ", parts[mid..]).ToUpperInvariant();
            }
        }

        private void InitializeBrandingSupport()
        {
            _settingsService.SettingsChanged += RefreshHeaderBranding;
        }

        private void RefreshHeaderBranding()
        {
            OnPropertyChanged(nameof(SchoolName));
            OnPropertyChanged(nameof(SchoolAddress));
            OnPropertyChanged(nameof(SystemTitle));
            OnPropertyChanged(nameof(SystemAcronym));
            OnPropertyChanged(nameof(DashboardLogoSource));
            OnPropertyChanged(nameof(SchoolNameFirstLine));
            OnPropertyChanged(nameof(SchoolNameSecondLine));
        }
    }
}
