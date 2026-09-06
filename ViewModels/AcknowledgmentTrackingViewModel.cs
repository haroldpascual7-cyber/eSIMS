using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class AcknowledgmentTrackingViewModel : ObservableObject
    {
        private readonly IAcknowledgmentService _ackService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private ObservableCollection<AcknowledgmentLog> _allLogs = new();
        private ObservableCollection<AcknowledgmentLog> _filteredLogs = new();
        private AcknowledgmentLog? _selectedLog;

        private string _searchText = string.Empty;
        private string _selectedStatusFilter = "All";

        private int _pendingCount;
        private int _overdueCount;
        private int _completedCount;

        // Log Receipt Modal
        private bool _isLogModalOpen;
        private AcknowledgmentLog? _editingLog;
        private string _selectedComplianceStatus = "Acknowledged - Signed Slip";
        private DateTime _dateAcknowledgedInput = DateTime.Now;
        private string _remarksInput = string.Empty;
        private string _attachedFilePathInput = string.Empty;
        private string _attachedFileName = "No File Selected";

        public AcknowledgmentTrackingViewModel(
            IAcknowledgmentService ackService,
            Action onBack,
            IPdfExportService? pdfService = null)
        {
            _ackService = ackService ?? throw new ArgumentNullException(nameof(ackService));
            _onBack     = onBack     ?? throw new ArgumentNullException(nameof(onBack));
            _pdfService = pdfService ?? new PdfExportService();

            BackToHubCommand          = new RelayCommand(_ => _onBack());
            ClearSearchCommand        = new RelayCommand(_ => SearchText = string.Empty);
            ExportOverdueReportCommand= new RelayCommand(_ => ExportOverdueReport());
            OpenLogModalCommand       = new RelayCommand(param => OpenLogModal(param as AcknowledgmentLog ?? SelectedLog));
            CloseLogModalCommand      = new RelayCommand(_ => IsLogModalOpen = false);
            UploadSlipCommand         = new RelayCommand(_ => UploadSlip());
            SaveAcknowledgmentCommand = new RelayCommand(async _ => await SaveAcknowledgmentAsync());

            StatusFilters = new List<string> { "All", "Pending", "Overdue", "Acknowledged" };

            _ = InitializeDataAsync();
        }

        public ObservableCollection<AcknowledgmentLog> AllLogs      { get => _allLogs; set => SetProperty(ref _allLogs, value); }
        public ObservableCollection<AcknowledgmentLog> FilteredLogs { get => _filteredLogs; set => SetProperty(ref _filteredLogs, value); }

        public AcknowledgmentLog? SelectedLog
        {
            get => _selectedLog;
            set => SetProperty(ref _selectedLog, value);
        }

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

        public List<string> StatusFilters { get; }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public int PendingCount   { get => _pendingCount; set => SetProperty(ref _pendingCount, value); }
        public int OverdueCount   { get => _overdueCount; set => SetProperty(ref _overdueCount, value); }
        public int CompletedCount { get => _completedCount; set => SetProperty(ref _completedCount, value); }

        // Modal Properties
        public bool IsLogModalOpen { get => _isLogModalOpen; set => SetProperty(ref _isLogModalOpen, value); }

        public AcknowledgmentLog? EditingLog
        {
            get => _editingLog;
            set => SetProperty(ref _editingLog, value);
        }

        public List<string> ComplianceStatuses => new()
        {
            "Acknowledged - Signed Slip",
            "Acknowledged - Attended Meeting",
            "Non-Compliant"
        };

        public string SelectedComplianceStatus { get => _selectedComplianceStatus; set => SetProperty(ref _selectedComplianceStatus, value); }
        public DateTime DateAcknowledgedInput  { get => _dateAcknowledgedInput; set => SetProperty(ref _dateAcknowledgedInput, value); }
        public string RemarksInput              { get => _remarksInput; set => SetProperty(ref _remarksInput, value); }
        public string AttachedFilePathInput    { get => _attachedFilePathInput; set => SetProperty(ref _attachedFilePathInput, value); }
        public string AttachedFileName         { get => _attachedFileName; set => SetProperty(ref _attachedFileName, value); }

        // Commands
        public ICommand BackToHubCommand          { get; }
        public ICommand ClearSearchCommand        { get; }
        public ICommand ExportOverdueReportCommand{ get; }
        public ICommand OpenLogModalCommand       { get; }
        public ICommand CloseLogModalCommand      { get; }
        public ICommand UploadSlipCommand         { get; }
        public ICommand SaveAcknowledgmentCommand { get; }

        public async Task InitializeDataAsync()
        {
            var logs = await _ackService.GetAllLogsAsync();
            AllLogs = new ObservableCollection<AcknowledgmentLog>(logs);
            UpdateKpiCounters();
            ApplyFilters();
        }

        private void ExportOverdueReport()
        {
            var overdue = AllLogs.Where(l => l.Status == "Overdue" || (l.Status == "Pending" && l.IsOverdue)).ToList();
            if (!overdue.Any())
            {
                MessageBox.Show("No overdue compliance records to export.", "Export Overdue Report", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"Overdue_Report_{DateTime.Now:yyyy-MM-dd}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Overdue Compliance Report PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateOverdueComplianceReport(dialog.FileName, overdue);
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating overdue report:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateKpiCounters()
        {
            PendingCount   = AllLogs.Count(l => l.Status == "Pending" && !l.IsOverdue);
            OverdueCount   = AllLogs.Count(l => l.Status == "Overdue" || (l.Status == "Pending" && l.IsOverdue));
            CompletedCount = AllLogs.Count(l => l.Status == "Acknowledged");
        }

        private void ApplyFilters()
        {
            var query = AllLogs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.Trim().ToLower();
                query = query.Where(l =>
                    (!string.IsNullOrEmpty(l.StudentName) && l.StudentName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(l.NoticeId) && l.NoticeId.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(l.Id) && l.Id.ToLower().Contains(search))
                );
            }

            if (SelectedStatusFilter != "All")
            {
                if (SelectedStatusFilter == "Overdue")
                {
                    query = query.Where(l => l.Status == "Overdue" || (l.Status == "Pending" && l.IsOverdue));
                }
                else if (SelectedStatusFilter == "Pending")
                {
                    query = query.Where(l => l.Status == "Pending" && !l.IsOverdue);
                }
                else
                {
                    query = query.Where(l => l.Status == SelectedStatusFilter);
                }
            }

            FilteredLogs = new ObservableCollection<AcknowledgmentLog>(query.ToList());
        }

        private void OpenLogModal(AcknowledgmentLog? log)
        {
            if (log == null) return;
            EditingLog = log;
            SelectedComplianceStatus = "Acknowledged - Signed Slip";
            DateAcknowledgedInput = DateTime.Now;
            RemarksInput = log.Remarks;
            AttachedFilePathInput = log.AttachedFilePath;
            AttachedFileName = string.IsNullOrWhiteSpace(log.AttachedFilePath) ? "No File Selected" : Path.GetFileName(log.AttachedFilePath);
            IsLogModalOpen = true;
        }

        private void UploadSlip()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Document Files (*.pdf;*.jpg;*.png)|*.pdf;*.jpg;*.png|All Files (*.*)|*.*",
                Title  = "Select Scanned Parent Acknowledgment Slip"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourcePath = openFileDialog.FileName;
                    string destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "Scanned_Slips");
                    Directory.CreateDirectory(destFolder);

                    string fileName = $"{Guid.NewGuid().ToString().Substring(0, 8)}_{Path.GetFileName(sourcePath)}";
                    string destPath = Path.Combine(destFolder, fileName);
                    File.Copy(sourcePath, destPath, true);

                    AttachedFilePathInput = destPath;
                    AttachedFileName = Path.GetFileName(sourcePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error attaching file:\n{ex.Message}", "File Attachment Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task SaveAcknowledgmentAsync()
        {
            if (EditingLog == null) return;

            string status = SelectedComplianceStatus.StartsWith("Acknowledged") ? "Acknowledged" : "Non-Compliant";
            string dateStr = DateAcknowledgedInput.ToString("yyyy-MM-dd");

            await _ackService.UpdateAcknowledgmentAsync(
                EditingLog.Id,
                status,
                dateStr,
                RemarksInput,
                AttachedFilePathInput);

            IsLogModalOpen = false;
            await InitializeDataAsync();
            MessageBox.Show($"Acknowledgment logged successfully for {EditingLog.StudentName}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}