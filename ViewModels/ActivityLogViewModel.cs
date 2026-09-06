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
    public class ActivityLogViewModel : ObservableObject
    {
        private readonly IAuditService _auditService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private ObservableCollection<AuditLogModel> _allLogs = new();
        private ICollectionView _filteredLogsView;

        private string _searchText = string.Empty;
        private string _selectedActionFilter = "All";
        private string _selectedModuleFilter = "All";
        private DateTime? _selectedDate;

        private int _totalRecordsCount;
        private bool _isLoading;

        public ActivityLogViewModel(
            IAuditService auditService,
            IPdfExportService pdfService,
            Action onBack)
        {
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _pdfService   = pdfService   ?? throw new ArgumentNullException(nameof(pdfService));
            _onBack        = onBack        ?? throw new ArgumentNullException(nameof(onBack));

            _filteredLogsView = CollectionViewSource.GetDefaultView(_allLogs);
            _filteredLogsView.Filter = FilterPredicate;

            BackToHubCommand         = new RelayCommand(_ => _onBack());
            ClearSearchCommand       = new RelayCommand(_ => SearchText = string.Empty);
            ResetFiltersCommand      = new RelayCommand(_ => ResetFilters());
            ExportAuditLedgerCommand = new RelayCommand(_ => ExportAuditLedger());

            ActionFilters = new List<string> { "All", "LOGIN", "LOGOUT", "CREATE", "UPDATE", "DELETE", "EXPORT" };
            ModuleFilters = new List<string> { "All", "Incident Intake", "Parent Notices", "Action Plans", "System Auth", "Reports Analytics", "Audit Trail" };

            _ = InitializeDataAsync();
        }

        public ObservableCollection<AuditLogModel> AllLogs { get => _allLogs; set => SetProperty(ref _allLogs, value); }
        public ICollectionView FilteredLogsView            { get => _filteredLogsView; set => SetProperty(ref _filteredLogsView, value); }

        public List<string> ActionFilters { get; }
        public List<string> ModuleFilters { get; }

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

        public string SelectedActionFilter
        {
            get => _selectedActionFilter;
            set
            {
                if (SetProperty(ref _selectedActionFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedModuleFilter
        {
            get => _selectedModuleFilter;
            set
            {
                if (SetProperty(ref _selectedModuleFilter, value))
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

        public int TotalRecordsCount { get => _totalRecordsCount; set => SetProperty(ref _totalRecordsCount, value); }
        public bool IsLoading         { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        // Commands
        public ICommand BackToHubCommand         { get; }
        public ICommand ClearSearchCommand       { get; }
        public ICommand ResetFiltersCommand      { get; }
        public ICommand ExportAuditLedgerCommand { get; }

        public async Task InitializeDataAsync()
        {
            IsLoading = true;
            try
            {
                var logs = await _auditService.GetAllAuditLogsAsync();
                AllLogs = new ObservableCollection<AuditLogModel>(logs);

                _filteredLogsView = CollectionViewSource.GetDefaultView(AllLogs);
                _filteredLogsView.Filter = FilterPredicate;

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not AuditLogModel log) return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.Trim().ToLower();
                bool matchesDesc = !string.IsNullOrEmpty(log.Description) && log.Description.ToLower().Contains(query);
                bool matchesUser = !string.IsNullOrEmpty(log.UserId) && log.UserId.ToLower().Contains(query);
                if (!matchesDesc && !matchesUser) return false;
            }

            if (SelectedActionFilter != "All" && !log.ActionType.Equals(SelectedActionFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (SelectedModuleFilter != "All" && !log.ModuleName.Equals(SelectedModuleFilter, StringComparison.OrdinalIgnoreCase))
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
            TotalRecordsCount = FilteredLogsView?.Cast<AuditLogModel>().Count() ?? 0;
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedActionFilter = "All";
            SelectedModuleFilter = "All";
            SelectedDate = null;
        }

        private void ExportAuditLedger()
        {
            var filteredList = FilteredLogsView.Cast<AuditLogModel>().ToList();
            if (!filteredList.Any())
            {
                MessageBox.Show("No audit logs available matching the filter to export.", "Export Audit Ledger", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"SecurityAudit_{DateTime.Now:yyyy-MM-dd}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save System Security Audit Ledger PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateAuditLedgerPdf(dialog.FileName, filteredList);
                    
                    _ = _auditService.LogActivityAsync("ADMIN-01", "EXPORT", "Audit Trail", $"Exported security audit ledger PDF with {filteredList.Count} entries.");

                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating audit ledger report:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}