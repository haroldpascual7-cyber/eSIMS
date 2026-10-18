using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class InvestigationDetailViewModel
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonText));
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonText));
                }
            }
        }

        public bool IsNotRefreshing => !IsRefreshing && !IsLoading;
        public string RefreshButtonText => (IsRefreshing || IsLoading) ? "⌛ Refreshing..." : "↻ Refresh";
        public string SaveButtonText => IsLoading ? "⌛ Saving..." : "💾 Save Investigation";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing || IsLoading) return;
            try
            {
                IsRefreshing = true;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
                await InitializeWorkspaceAsync();
                await Task.Delay(750);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing investigation: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }
    }
}
