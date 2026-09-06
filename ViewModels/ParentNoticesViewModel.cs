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
    public partial class ParentNoticesViewModel : ObservableObject
    {
        private readonly IAcknowledgmentService _ackService;
        private readonly ICommunicationService _commService;
        private readonly ILetterTemplateService _templateService;
        private readonly IStudentService _studentService;
        private readonly IPdfExportService _pdfService;

        private List<Student> _allStudents = new();
        private List<AcknowledgmentLog> _allNotices = new();
        private ObservableCollection<AcknowledgmentLog> _filteredNotices = new();
        private ObservableCollection<DispatchLog> _recentDispatches = new();
        private ObservableCollection<LetterTemplate> _templates = new();

        private AcknowledgmentLog? _selectedNotice;
        private DispatchLog? _selectedDispatch;
        private LetterTemplate? _selectedTemplate;

        private string _searchText = string.Empty;
        private string _selectedStatusFilter = "All";
        private int _selectedTabIndex;
        private bool _isLoading;

        private int _totalDispatchedCount;
        private int _pendingCount;
        private int _overdueCount;
        private int _acknowledgedCount;

        // --- Create Notice Modal State ---
        private bool _isCreateNoticeModalOpen;
        private string _studentSearchInput = string.Empty;
        private ObservableCollection<Student> _studentSuggestions = new();
        private bool _isStudentSuggestionsOpen;
        private Student? _selectedNoticeStudent;
        private string _guardianName = string.Empty;
        private string _guardianEmail = string.Empty;
        private string _guardianPhone = string.Empty;
        private string _selectedChannel = "Email";
        private string _noticeSubject = string.Empty;
        private string _noticeBody = string.Empty;
        private LetterTemplate? _modalSelectedTemplate;

        // --- Log Receipt Modal State ---
        private bool _isLogReceiptModalOpen;
        private AcknowledgmentLog? _editingLog;
        private string _selectedComplianceStatus = "Acknowledged";
        private DateTime _dateAcknowledged = DateTime.Today;
        private string _slipRemarks = string.Empty;
        private string _attachedFilePath = string.Empty;

        public ParentNoticesViewModel(
            IAcknowledgmentService ackService,
            ICommunicationService commService,
            ILetterTemplateService templateService,
            IStudentService studentService,
            IPdfExportService pdfService)
        {
            _ackService = ackService ?? throw new ArgumentNullException(nameof(ackService));
            _commService = commService ?? throw new ArgumentNullException(nameof(commService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));

            StatusFilters = new List<string> { "All", "Pending", "Overdue", "Acknowledged" };
            ChannelOptions = new List<string> { "Email", "SMS", "Both" };
            ComplianceOptions = new List<string> { "Acknowledged", "Non-Compliant", "Pending" };

            // Commands
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            OpenCreateNoticeCommand = new RelayCommand(OpenCreateNotice, () => CanManageNotices);
            CloseCreateNoticeCommand = new RelayCommand(() => IsCreateNoticeModalOpen = false);
            SendNoticeCommand = new RelayCommand(async () => await SendNoticeAsync());

            OpenLogReceiptCommand = new RelayCommand(param => OpenLogReceipt(param as AcknowledgmentLog ?? SelectedNotice), _ => CanManageNotices);
            CloseLogReceiptCommand = new RelayCommand(() => IsLogReceiptModalOpen = false);
            BrowseSlipFileCommand = new RelayCommand(BrowseSlipFile);
            SaveReceiptCommand = new RelayCommand(async () => await SaveReceiptAsync());

            ExportNoticePdfCommand = new RelayCommand(param => ExportNoticePdf(param as AcknowledgmentLog ?? SelectedNotice));
            ExportOverdueReportCommand = new RelayCommand(ExportOverdueReport);

            SelectStudentCommand = new RelayCommand(param =>
            {
                if (param is Student s) SelectStudent(s);
            });

            _ = LoadDataAsync();
        }

        public bool CanManageNotices => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");

        public ObservableCollection<AcknowledgmentLog> FilteredNotices => _filteredNotices;
        public ObservableCollection<DispatchLog> RecentDispatches => _recentDispatches;
        public ObservableCollection<LetterTemplate> Templates => _templates;
        public ObservableCollection<Student> StudentSuggestions => _studentSuggestions;

        public List<string> StatusFilters { get; }
        public List<string> ChannelOptions { get; }
        public List<string> ComplianceOptions { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilters();
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                    ApplyFilters();
            }
        }

        public AcknowledgmentLog? SelectedNotice
        {
            get => _selectedNotice;
            set => SetProperty(ref _selectedNotice, value);
        }

        public DispatchLog? SelectedDispatch
        {
            get => _selectedDispatch;
            set => SetProperty(ref _selectedDispatch, value);
        }

        public LetterTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set => SetProperty(ref _selectedTemplate, value);
        }

        public int TotalDispatchedCount { get => _totalDispatchedCount; set => SetProperty(ref _totalDispatchedCount, value); }
        public int PendingCount { get => _pendingCount; set => SetProperty(ref _pendingCount, value); }
        public int OverdueCount { get => _overdueCount; set => SetProperty(ref _overdueCount, value); }
        public int AcknowledgedCount { get => _acknowledgedCount; set => SetProperty(ref _acknowledgedCount, value); }

        // --- Create Notice Modal Properties ---
        public bool IsCreateNoticeModalOpen { get => _isCreateNoticeModalOpen; set => SetProperty(ref _isCreateNoticeModalOpen, value); }

        public string StudentSearchInput
        {
            get => _studentSearchInput;
            set
            {
                if (SetProperty(ref _studentSearchInput, value))
                    UpdateStudentSuggestions();
            }
        }

        public bool IsStudentSuggestionsOpen { get => _isStudentSuggestionsOpen; set => SetProperty(ref _isStudentSuggestionsOpen, value); }

        public Student? SelectedNoticeStudent
        {
            get => _selectedNoticeStudent;
            set => SetProperty(ref _selectedNoticeStudent, value);
        }

        public string GuardianName { get => _guardianName; set => SetProperty(ref _guardianName, value); }
        public string GuardianEmail { get => _guardianEmail; set => SetProperty(ref _guardianEmail, value); }
        public string GuardianPhone { get => _guardianPhone; set => SetProperty(ref _guardianPhone, value); }
        public string SelectedChannel { get => _selectedChannel; set => SetProperty(ref _selectedChannel, value); }
        public string NoticeSubject { get => _noticeSubject; set => SetProperty(ref _noticeSubject, value); }
        public string NoticeBody { get => _noticeBody; set => SetProperty(ref _noticeBody, value); }

        public LetterTemplate? ModalSelectedTemplate
        {
            get => _modalSelectedTemplate;
            set
            {
                if (SetProperty(ref _modalSelectedTemplate, value) && value != null)
                {
                    NoticeSubject = value.TemplateName;
                    ApplyTemplatePlaceholders(value.TemplateBody);
                }
            }
        }

        // --- Log Receipt Modal Properties ---
        public bool IsLogReceiptModalOpen { get => _isLogReceiptModalOpen; set => SetProperty(ref _isLogReceiptModalOpen, value); }
        public AcknowledgmentLog? EditingLog { get => _editingLog; set => SetProperty(ref _editingLog, value); }
        public string SelectedComplianceStatus { get => _selectedComplianceStatus; set => SetProperty(ref _selectedComplianceStatus, value); }
        public DateTime DateAcknowledged { get => _dateAcknowledged; set => SetProperty(ref _dateAcknowledged, value); }
        public string SlipRemarks { get => _slipRemarks; set => SetProperty(ref _slipRemarks, value); }
        public string AttachedFilePath { get => _attachedFilePath; set => SetProperty(ref _attachedFilePath, value); }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateNoticeCommand { get; }
        public ICommand CloseCreateNoticeCommand { get; }
        public ICommand SendNoticeCommand { get; }
        public ICommand OpenLogReceiptCommand { get; }
        public ICommand CloseLogReceiptCommand { get; }
        public ICommand BrowseSlipFileCommand { get; }
        public ICommand SaveReceiptCommand { get; }
        public ICommand ExportNoticePdfCommand { get; }
        public ICommand ExportOverdueReportCommand { get; }
        public ICommand SelectStudentCommand { get; }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                _allStudents = await _studentService.GetAllStudentsAsync();
                _allNotices = await _ackService.GetAllLogsAsync();

                var dispatches = await _commService.GetRecentDispatchesAsync(100);
                _recentDispatches.Clear();
                foreach (var d in dispatches) _recentDispatches.Add(d);

                var tpls = await _templateService.GetAllTemplatesAsync();
                _templates.Clear();
                foreach (var t in tpls) _templates.Add(t);

                TotalDispatchedCount = dispatches.Count;
                PendingCount = _allNotices.Count(n => n.DisplayStatus == "Pending");
                OverdueCount = _allNotices.Count(n => n.DisplayStatus == "Overdue");
                AcknowledgedCount = _allNotices.Count(n => n.Status == "Acknowledged");

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var query = _allNotices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim().ToLower();
                query = query.Where(n =>
                    (n.NoticeId != null && n.NoticeId.ToLower().Contains(term)) ||
                    (n.StudentName != null && n.StudentName.ToLower().Contains(term)) ||
                    (n.Remarks != null && n.Remarks.ToLower().Contains(term)));
            }

            if (SelectedStatusFilter != "All")
            {
                query = query.Where(n => n.DisplayStatus.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            _filteredNotices.Clear();
            foreach (var item in query.OrderByDescending(n => n.IssueDate))
                _filteredNotices.Add(item);
        }

        private void UpdateStudentSuggestions()
        {
            _studentSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(StudentSearchInput) || StudentSearchInput.Length < 2)
            {
                IsStudentSuggestionsOpen = false;
                return;
            }

            string term = StudentSearchInput.Trim().ToLower();
            var matches = _allStudents
                .Where(s => s.FullName.ToLower().Contains(term) || s.StudentNumber.ToLower().Contains(term))
                .Take(6)
                .ToList();

            foreach (var m in matches) _studentSuggestions.Add(m);
            IsStudentSuggestionsOpen = _studentSuggestions.Count > 0;
        }

        private void SelectStudent(Student s)
        {
            SelectedNoticeStudent = s;
            StudentSearchInput = s.FullName;
            GuardianName = !string.IsNullOrWhiteSpace(s.GuardianName) ? s.GuardianName : "Parent/Guardian";
            GuardianEmail = s.GuardianEmail ?? string.Empty;
            GuardianPhone = s.GuardianContact ?? string.Empty;
            IsStudentSuggestionsOpen = false;

            if (ModalSelectedTemplate != null)
                ApplyTemplatePlaceholders(ModalSelectedTemplate.TemplateBody);
        }

        private void ApplyTemplatePlaceholders(string body)
        {
            if (string.IsNullOrEmpty(body)) return;

            string sName = SelectedNoticeStudent?.FullName ?? "[Student Name]";
            string sId = SelectedNoticeStudent?.StudentNumber ?? "[Student ID]";
            string gSec = SelectedNoticeStudent != null ? $"{SelectedNoticeStudent.GradeLevel} - {SelectedNoticeStudent.Section}" : "[Grade & Section]";
            string dateStr = DateTime.Today.ToString("MMMM dd, yyyy");

            string rendered = body
                .Replace("{{StudentName}}", sName)
                .Replace("{{StudentId}}", sId)
                .Replace("{{GradeSection}}", gSec)
                .Replace("{{ActionDate}}", dateStr)
                .Replace("{{IncidentDate}}", dateStr)
                .Replace("{{Location}}", "Campus Grounds")
                .Replace("{{Infraction}}", "Behavioral Conduct Notice")
                .Replace("{{OfficerName}}", SessionManager.FullName);

            NoticeBody = rendered;
        }

        private void OpenCreateNotice()
        {
            SelectedNoticeStudent = null;
            StudentSearchInput = string.Empty;
            GuardianName = string.Empty;
            GuardianEmail = string.Empty;
            GuardianPhone = string.Empty;
            NoticeSubject = "Formal Notice: Incident & Conduct Review";
            NoticeBody = string.Empty;
            SelectedChannel = "Email";
            ModalSelectedTemplate = Templates.FirstOrDefault();

            IsCreateNoticeModalOpen = true;
        }

        private async Task SendNoticeAsync()
        {
            if (SelectedNoticeStudent == null)
            {
                MessageBox.Show("Please select a student.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NoticeBody))
            {
                MessageBox.Show("Please enter the notice body.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool success = true;
                if (SelectedChannel == "Email" || SelectedChannel == "Both")
                {
                    if (string.IsNullOrWhiteSpace(GuardianEmail))
                    {
                        MessageBox.Show("Guardian email address is missing for this student.", "Notice Dispatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    success &= await _commService.SendEmailAsync(SelectedNoticeStudent.StudentNumber, GuardianName, GuardianEmail, NoticeSubject, NoticeBody);
                }

                if (SelectedChannel == "SMS" || SelectedChannel == "Both")
                {
                    if (!string.IsNullOrWhiteSpace(GuardianPhone))
                        success &= await _commService.SendSmsAsync(SelectedNoticeStudent.StudentNumber, GuardianName, GuardianPhone, NoticeBody);
                }

                string noticeId = $"NTC-{DateTime.Now:yyyy}-{new Random().Next(1000, 9999)}";
                var ack = new AcknowledgmentLog
                {
                    Id = Guid.NewGuid().ToString(),
                    NoticeId = noticeId,
                    StudentName = SelectedNoticeStudent.FullName,
                    IssueDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    DueDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"),
                    Status = "Pending",
                    Remarks = $"Dispatched via {SelectedChannel} to {GuardianName} ({GuardianEmail})"
                };

                await _ackService.SaveLogAsync(ack);

                IsCreateNoticeModalOpen = false;
                await LoadDataAsync();
                MessageBox.Show($"Parent notice successfully sent!\nNotice Reference: {noticeId}", "Notice Dispatched", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send notice: {ex.Message}", "Dispatch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenLogReceipt(AcknowledgmentLog? log)
        {
            if (log == null) return;
            EditingLog = log;
            SelectedComplianceStatus = log.Status == "Pending" ? "Acknowledged" : log.Status;
            DateAcknowledged = DateTime.Today;
            SlipRemarks = log.Remarks ?? string.Empty;
            AttachedFilePath = log.AttachedFilePath ?? string.Empty;

            IsLogReceiptModalOpen = true;
        }

        private void BrowseSlipFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Signed Acknowledgment Slip File",
                Filter = "Documents and Images (*.pdf;*.jpg;*.png)|*.pdf;*.jpg;*.png|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
                AttachedFilePath = dlg.FileName;
        }

        private async Task SaveReceiptAsync()
        {
            if (EditingLog == null) return;

            try
            {
                string dateStr = DateAcknowledged.ToString("yyyy-MM-dd");
                await _ackService.UpdateAcknowledgmentAsync(EditingLog.Id, SelectedComplianceStatus, dateStr, SlipRemarks.Trim(), AttachedFilePath.Trim());

                IsLogReceiptModalOpen = false;
                await LoadDataAsync();
                MessageBox.Show("Acknowledgment receipt updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update acknowledgment: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportNoticePdf(AcknowledgmentLog? log)
        {
            if (log == null) return;
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Notice_{log.NoticeId}.pdf");
                string body = string.IsNullOrWhiteSpace(log.Remarks) ? $"Official Disciplinary Notice issued to {log.StudentName} regarding school behavioral policy." : log.Remarks;
                _pdfService.GenerateParentNoticePdf(path, body, SessionManager.FullName, $"NOTICE REF: {log.NoticeId}");
                MessageBox.Show($"Official Notice PDF exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF Export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportOverdueReport()
        {
            try
            {
                var overdueList = _allNotices.Where(n => n.DisplayStatus == "Overdue").ToList();
                if (overdueList.Count == 0)
                {
                    MessageBox.Show("There are no overdue parent notices at this time.", "Compliance Report", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Overdue_Parent_Notices_{DateTime.Today:yyyyMMdd}.pdf");
                _pdfService.GenerateOverdueComplianceReport(path, overdueList);
                MessageBox.Show($"Overdue Compliance Report exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
