using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class SecurityAccessLogViewModel : ObservableObject
    {
        private readonly ISecurityAccessLogService _logService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private ObservableCollection<AccessSecurityLogModel> _allLogs = new();
        private ICollectionView _filteredLogsView;
        private AccessSecurityLogModel? _selectedLogItem;

        private string _searchText = string.Empty;
        private string _selectedEventTypeFilter = "All";
        private DateTime? _selectedDate;

        private int _activeSessionsCount;
        private int _failedLogins24hCount;
        private int _lockedAccountsCount;
        private bool _isLoading;

        public SecurityAccessLogViewModel(
            ISecurityAccessLogService logService,
            IPdfExportService pdfService,
            Action onBack)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _onBack     = onBack     ?? throw new ArgumentNullException(nameof(onBack));

            _filteredLogsView = CollectionViewSource.GetDefaultView(_allLogs);
            _filteredLogsView.Filter = FilterPredicate;

            BackToHubCommand            = new RelayCommand(_ => _onBack());
            ClearSearchCommand          = new RelayCommand(_ => SearchText = string.Empty);
            ResetFiltersCommand         = new RelayCommand(_ => ResetFilters());
            UnlockAccountCommand        = new RelayCommand(async _ => await UnlockAccountAsync(), _ => CanUnlockAccount);
            ExportSecurityReportCommand = new RelayCommand(_ => ExportSecurityReport());

            EventTypeFilters = new List<string> { "All", "Login", "Logout", "Failed Login", "Role Change", "Account Lockout", "Account Unlocked" };

            _ = InitializeDataAsync();
        }

        public ObservableCollection<AccessSecurityLogModel> AllLogs { get => _allLogs; set => SetProperty(ref _allLogs, value); }
        public ICollectionView FilteredLogsView            { get => _filteredLogsView; set => SetProperty(ref _filteredLogsView, value); }

        public List<string> EventTypeFilters { get; }

        public AccessSecurityLogModel? SelectedLogItem
        {
            get => _selectedLogItem;
            set
            {
                if (SetProperty(ref _selectedLogItem, value))
                {
                    OnPropertyChanged(nameof(CanUnlockAccount));
                }
            }
        }

        public bool CanUnlockAccount => SelectedLogItem != null && (SelectedLogItem.IsLockedAccount || SelectedLogItem.EventType.Contains("Failed"));

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChanged(nameof(HasSearchText));
                    ApplyFilters();
                }
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        public string SelectedEventTypeFilter
        {
            get => _selectedEventTypeFilter;
            set
            {
                if (SetProperty(ref _selectedEventTypeFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    ApplyFilters();
                }
            }
        }

        public int ActiveSessionsCount  { get => _activeSessionsCount; set => SetProperty(ref _activeSessionsCount, value); }
        public int FailedLogins24hCount { get => _failedLogins24hCount; set => SetProperty(ref _failedLogins24hCount, value); }
        public int LockedAccountsCount  { get => _lockedAccountsCount; set => SetProperty(ref _lockedAccountsCount, value); }
        public bool IsLoading           { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        // Commands
        public ICommand BackToHubCommand            { get; }
        public ICommand ClearSearchCommand          { get; }
        public ICommand ResetFiltersCommand         { get; }
        public ICommand UnlockAccountCommand        { get; }
        public ICommand ExportSecurityReportCommand { get; }

        public async Task InitializeDataAsync()
        {
            IsLoading = true;
            try
            {
                var logs = await _logService.GetAccessLogsAsync();
                AllLogs = new ObservableCollection<AccessSecurityLogModel>(logs);

                _filteredLogsView = CollectionViewSource.GetDefaultView(AllLogs);
                _filteredLogsView.Filter = FilterPredicate;

                // Calculate KPI metrics
                ActiveSessionsCount = logs.Count(l => l.EventType == "Login" && l.Timestamp >= DateTime.Now.AddHours(-8));
                FailedLogins24hCount = logs.Count(l => l.EventType == "Failed Login" && l.Timestamp >= DateTime.Now.AddHours(-24));
                LockedAccountsCount = logs.Count(l => l.IsLockedAccount);

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not AccessSecurityLogModel log) return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string q = SearchText.Trim().ToLower();
                bool matchUser = !string.IsNullOrEmpty(log.AttemptedUsername) && log.AttemptedUsername.ToLower().Contains(q);
                bool matchMach = !string.IsNullOrEmpty(log.MachineName) && log.MachineName.ToLower().Contains(q);
                if (!matchUser && !matchMach) return false;
            }

            if (SelectedEventTypeFilter != "All" && !log.EventType.Equals(SelectedEventTypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (SelectedDate.HasValue && log.Timestamp.Date != SelectedDate.Value.Date)
            {
                return false;
            }

            return true;
        }

        private void ApplyFilters()
        {
            FilteredLogsView?.Refresh();
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedEventTypeFilter = "All";
            SelectedDate = null;
        }

        private async Task UnlockAccountAsync()
        {
            if (SelectedLogItem == null) return;

            string targetUser = SelectedLogItem.AttemptedUsername;
            var result = MessageBox.Show($"Are you sure you want to unlock user account '{targetUser}'?", "Confirm Unlock Account", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            bool success = await _logService.UnlockAccountAsync(targetUser);
            if (success)
            {
                _ = AuditLogger.LogAsync("ADMIN-01", "UPDATE", "Security & Access Log", $"Unlocked user account '{targetUser}'.");
                MessageBox.Show($"Account '{targetUser}' has been unlocked successfully!", "Unlock Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await InitializeDataAsync();
            }
            else
            {
                MessageBox.Show($"Failed to unlock account '{targetUser}'.", "Unlock Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSecurityReport()
        {
            var filteredList = FilteredLogsView.Cast<AccessSecurityLogModel>().ToList();
            if (!filteredList.Any())
            {
                MessageBox.Show("No security access log records available to export.", "Export Report", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"SecurityAccessReport_{DateTime.Now:yyyy-MM-dd}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Security Access Log Report PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateSecurityAccessReportPdf(dialog.FileName, filteredList);

                    _ = AuditLogger.LogAsync("ADMIN-01", "EXPORT", "Security & Access Log", $"Exported security access report PDF with {filteredList.Count} entries.");

                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating security report:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}