using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class StudentsViewModel
    {
        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonIcon));
                }
            }
        }

        public bool IsNotRefreshing => !IsRefreshing && !IsLoading;
        public string RefreshButtonIcon => (IsRefreshing || IsLoading) ? "⌛" : "↻";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing || IsLoading) return;
            try
            {
                IsRefreshing = true;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonIcon));
                await Task.Yield();
                var list = await _studentService.SearchStudentsAsync(SearchText, SelectedGradeFilter, SelectedSectionFilter);
                Students.Clear();
                foreach (var s in list)
                {
                    Students.Add(s);
                }
                await Task.Delay(750);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing students: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonIcon));
            }
        }
    }
}
