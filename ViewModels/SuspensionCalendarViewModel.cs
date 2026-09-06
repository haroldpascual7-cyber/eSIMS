using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class SuspensionCalendarViewModel : ObservableObject
    {
        private readonly ISuspensionService _suspensionService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onBackToOverview;

        private string _searchText = string.Empty;
        private string _selectedTypeFilter = "All";
        private string _selectedStatusFilter = "All";
        private string _statusMessage = string.Empty;
        private DateTime? _selectedCalendarDate = DateTime.Now;

        // KPIs
        private int _activeSuspensionsCount;
        private int _issCount;
        private int _ossCount;
        private int _pendingClearanceCount;

        // Modal 1: File Order State
        private bool _isFileModalOpen;
        private string _fileStudentName = "Michael Brown";
        private string _fileStudentId = "STD-1042";
        private string _fileIncidentNumber = "INC-2026-00004";
        private string _fileSuspensionType = "Out-of-School Suspension (OSS)";
        private DateTime _fileStartDate = DateTime.Now;
        private DateTime _fileEndDate = DateTime.Now.AddDays(5);
        private DateTime _fileReentryDate = DateTime.Now.AddDays(6);
        private string _fileIssuedBy = "Dean of Student Affairs";
        private string _fileTerms = string.Empty;
        private bool _fileParentConferenceRequired = true;
        private bool _fileAcademicPacketRequired = true;
        private bool _fileCounselorIntakeRequired = true;
        private string _fileErrorMessage = string.Empty;

        // Modal 2: Checklist State
        private bool _isChecklistModalOpen;
        private SuspensionOrder? _selectedOrder;
        private bool _checklistParentPassed;
        private bool _checklistPacketPassed;
        private bool _checklistCounselorPassed;
        private string _checklistNotes = string.Empty;
        private string _checklistErrorMessage = string.Empty;

        public SuspensionCalendarViewModel(
            ISuspensionService suspensionService,
            IIncidentService incidentService,
            Action onBackToOverview)
        {
            _suspensionService = suspensionService ?? throw new ArgumentNullException(nameof(suspensionService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _onBackToOverview = onBackToOverview ?? throw new ArgumentNullException(nameof(onBackToOverview));

            SuspensionOrders = new ObservableCollection<SuspensionOrder>();
            AvailableIncidents = new ObservableCollection<string>();
            AvailableStudents = new ObservableCollection<string>();

            TypeFilterOptions = new ObservableCollection<string> { "All", "ISS", "OSS", "Preventative Exclusion" };
            StatusFilterOptions = new ObservableCollection<string> { "All", "Active Ban", "Pending Review", "Readmitted" };
            SuspensionTypeOptions = new ObservableCollection<string>
            {
                "In-School Suspension (ISS)",
                "Out-of-School Suspension (OSS)",
                "Preventative Exclusion"
            };

            BackToOverviewCommand = new RelayCommand(() => _onBackToOverview());
            OpenFileModalCommand = new RelayCommand(OpenFileModal);
            CloseFileModalCommand = new RelayCommand(CloseFileModal);
            SaveNewOrderCommand = new RelayCommand(async () => await SaveNewOrderAsync());
            OpenChecklistModalCommand = new RelayCommand(param => { if (param is SuspensionOrder order) OpenChecklistModal(order); });
            CloseChecklistModalCommand = new RelayCommand(CloseChecklistModal);
            SaveChecklistCommand = new RelayCommand(async () => await SaveChecklistAsync());
            GrantFinalClearanceCommand = new RelayCommand(async () => await GrantFinalClearanceAsync());
            PrintNoticeCommand = new RelayCommand(param => { if (param is SuspensionOrder order) PrintNotice(order); });

            FilterTodayCommand = new RelayCommand(() => { SelectedCalendarDate = DateTime.Now; StatusMessage = "Filtered by Today's active suspensions."; });
            FilterThisWeekCommand = new RelayCommand(() => { SelectedCalendarDate = DateTime.Now; StatusMessage = "Filtered by This Week's active suspensions."; });
            FilterThisMonthCommand = new RelayCommand(() => { SelectedCalendarDate = DateTime.Now; StatusMessage = "Filtered by This Month's active suspensions."; });

            _ = InitializeDataAsync();
        }

        public ObservableCollection<SuspensionOrder> SuspensionOrders { get; }
        public ObservableCollection<string> AvailableIncidents { get; }
        public ObservableCollection<string> AvailableStudents { get; }

        public ObservableCollection<string> TypeFilterOptions { get; }
        public ObservableCollection<string> StatusFilterOptions { get; }
        public ObservableCollection<string> SuspensionTypeOptions { get; }

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _ = FilterOrdersAsync(); }
        }

        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set { if (SetProperty(ref _selectedTypeFilter, value)) _ = FilterOrdersAsync(); }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { if (SetProperty(ref _selectedStatusFilter, value)) _ = FilterOrdersAsync(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DateTime? SelectedCalendarDate
        {
            get => _selectedCalendarDate;
            set { if (SetProperty(ref _selectedCalendarDate, value)) _ = FilterOrdersAsync(); }
        }

        // KPIs
        public int ActiveSuspensionsCount { get => _activeSuspensionsCount; set => SetProperty(ref _activeSuspensionsCount, value); }
        public int IssCount { get => _issCount; set => SetProperty(ref _issCount, value); }
        public int OssCount { get => _ossCount; set => SetProperty(ref _ossCount, value); }
        public int PendingClearanceCount { get => _pendingClearanceCount; set => SetProperty(ref _pendingClearanceCount, value); }

        // Modal 1 Properties
        public bool IsFileModalOpen { get => _isFileModalOpen; set => SetProperty(ref _isFileModalOpen, value); }
        public string FileStudentName { get => _fileStudentName; set => SetProperty(ref _fileStudentName, value); }
        public string FileStudentId { get => _fileStudentId; set => SetProperty(ref _fileStudentId, value); }
        public string FileIncidentNumber { get => _fileIncidentNumber; set => SetProperty(ref _fileIncidentNumber, value); }
        public string FileSuspensionType { get => _fileSuspensionType; set => SetProperty(ref _fileSuspensionType, value); }
        public DateTime FileStartDate { get => _fileStartDate; set { if (SetProperty(ref _fileStartDate, value)) RecalculateDates(); } }
        public DateTime FileEndDate { get => _fileEndDate; set { if (SetProperty(ref _fileEndDate, value)) RecalculateDates(); } }
        public DateTime FileReentryDate { get => _fileReentryDate; set => SetProperty(ref _fileReentryDate, value); }
        public string FileIssuedBy { get => _fileIssuedBy; set => SetProperty(ref _fileIssuedBy, value); }
        public string FileTerms { get => _fileTerms; set => SetProperty(ref _fileTerms, value); }
        public bool FileParentConferenceRequired { get => _fileParentConferenceRequired; set => SetProperty(ref _fileParentConferenceRequired, value); }
        public bool FileAcademicPacketRequired { get => _fileAcademicPacketRequired; set => SetProperty(ref _fileAcademicPacketRequired, value); }
        public bool FileCounselorIntakeRequired { get => _fileCounselorIntakeRequired; set => SetProperty(ref _fileCounselorIntakeRequired, value); }
        public string FileErrorMessage { get => _fileErrorMessage; set => SetProperty(ref _fileErrorMessage, value); }

        // Modal 2 Properties
        public bool IsChecklistModalOpen { get => _isChecklistModalOpen; set => SetProperty(ref _isChecklistModalOpen, value); }
        public SuspensionOrder? SelectedOrder { get => _selectedOrder; set => SetProperty(ref _selectedOrder, value); }
        public bool ChecklistParentPassed { get => _checklistParentPassed; set => SetProperty(ref _checklistParentPassed, value); }
        public bool ChecklistPacketPassed { get => _checklistPacketPassed; set => SetProperty(ref _checklistPacketPassed, value); }
        public bool ChecklistCounselorPassed { get => _checklistCounselorPassed; set => SetProperty(ref _checklistCounselorPassed, value); }
        public string ChecklistNotes { get => _checklistNotes; set => SetProperty(ref _checklistNotes, value); }
        public string ChecklistErrorMessage { get => _checklistErrorMessage; set => SetProperty(ref _checklistErrorMessage, value); }

        // Commands
        public ICommand BackToOverviewCommand { get; }
        public ICommand OpenFileModalCommand { get; }
        public ICommand CloseFileModalCommand { get; }
        public ICommand SaveNewOrderCommand { get; }
        public ICommand OpenChecklistModalCommand { get; }
        public ICommand CloseChecklistModalCommand { get; }
        public ICommand SaveChecklistCommand { get; }
        public ICommand GrantFinalClearanceCommand { get; }
        public ICommand PrintNoticeCommand { get; }
        public ICommand FilterTodayCommand { get; }
        public ICommand FilterThisWeekCommand { get; }
        public ICommand FilterThisMonthCommand { get; }

        private async Task InitializeDataAsync()
        {
            var incidents = await _incidentService.GetAllIncidentsAsync();
            AvailableIncidents.Clear();
            foreach (var inc in incidents) AvailableIncidents.Add(inc.IncidentNumber);
            if (AvailableIncidents.Any()) FileIncidentNumber = AvailableIncidents.First();

            var students = await _incidentService.GetAllStudentsAsync();
            AvailableStudents.Clear();
            foreach (var st in students) AvailableStudents.Add($"{st.FullName} ({st.StudentNumber})");

            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            var orders = await _suspensionService.GetAllSuspensionOrdersAsync();
            UpdateOrdersCollection(orders);
        }

        private async Task FilterOrdersAsync()
        {
            var orders = await _suspensionService.SearchAndFilterOrdersAsync(SearchText, SelectedTypeFilter, SelectedStatusFilter);
            UpdateOrdersCollection(orders);
        }

        private void UpdateOrdersCollection(IEnumerable<SuspensionOrder> orders)
        {
            SuspensionOrders.Clear();
            var list = orders.ToList();
            foreach (var s in list) SuspensionOrders.Add(s);

            ActiveSuspensionsCount = list.Count(s => s.Status == "Active Ban");
            IssCount = list.Count(s => s.SuspensionType.Contains("ISS"));
            OssCount = list.Count(s => s.SuspensionType.Contains("OSS"));
            PendingClearanceCount = list.Count(s => s.Status == "Pending Review" || (!s.IsCleared && s.Status == "Active Ban"));
        }

        private void RecalculateDates()
        {
            if (FileEndDate >= FileStartDate) FileReentryDate = FileEndDate.AddDays(1);
        }

        private void OpenFileModal()
        {
            FileErrorMessage = string.Empty;
            FileStudentName = "Michael Brown";
            FileStudentId = "STD-1042";
            FileSuspensionType = "Out-of-School Suspension (OSS)";
            FileStartDate = DateTime.Now;
            FileEndDate = DateTime.Now.AddDays(5);
            FileReentryDate = DateTime.Now.AddDays(6);
            FileIssuedBy = "Dean of Student Affairs";
            FileTerms = string.Empty;
            IsFileModalOpen = true;
        }

        private void CloseFileModal() => IsFileModalOpen = false;

        private async Task SaveNewOrderAsync()
        {
            FileErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(FileStudentName)) { FileErrorMessage = "Please enter student's name."; return; }
            if (FileEndDate < FileStartDate) { FileErrorMessage = "End Date cannot be earlier than Start Date."; return; }
            if (string.IsNullOrWhiteSpace(FileTerms)) { FileErrorMessage = "Please specify suspension terms & parameters."; return; }

            int totalDays = (int)(FileEndDate - FileStartDate).TotalDays + 1;
            var order = new SuspensionOrder
            {
                OrderId = await _suspensionService.GenerateOrderIdAsync(),
                IncidentNumber = string.IsNullOrWhiteSpace(FileIncidentNumber) ? "INC-2026-00004" : FileIncidentNumber,
                StudentName = FileStudentName.Trim(),
                StudentIdNumber = string.IsNullOrWhiteSpace(FileStudentId) ? "STD-1042" : FileStudentId.Trim(),
                SuspensionType = FileSuspensionType,
                StartDate = FileStartDate,
                EndDate = FileEndDate,
                TotalDays = Math.Max(1, totalDays),
                ReentryDate = FileReentryDate,
                IssuedBy = FileIssuedBy.Trim(),
                Terms = FileTerms.Trim(),
                ParentConferenceRequired = FileParentConferenceRequired,
                AcademicPacketRequired = FileAcademicPacketRequired,
                CounselorIntakeRequired = FileCounselorIntakeRequired,
                ParentConferencePassed = false,
                AcademicPacketPassed = false,
                CounselorIntakePassed = false,
                IsCleared = false,
                Status = "Active Ban"
            };

            await _suspensionService.AddSuspensionOrderAsync(order);
            IsFileModalOpen = false;
            StatusMessage = $"✔ Suspension Order {order.OrderId} issued for {order.StudentName}.";
            await LoadOrdersAsync();
        }

        private void OpenChecklistModal(SuspensionOrder order)
        {
            SelectedOrder = order;
            ChecklistErrorMessage = string.Empty;
            ChecklistParentPassed = order.ParentConferencePassed;
            ChecklistPacketPassed = order.AcademicPacketPassed;
            ChecklistCounselorPassed = order.CounselorIntakePassed;
            ChecklistNotes = order.ClearanceNotes;
            IsChecklistModalOpen = true;
        }

        private void CloseChecklistModal() => IsChecklistModalOpen = false;

        private async Task SaveChecklistAsync()
        {
            if (SelectedOrder == null) return;
            await _suspensionService.UpdateClearanceStatusAsync(SelectedOrder.Id, ChecklistParentPassed, ChecklistPacketPassed, ChecklistCounselorPassed, ChecklistNotes.Trim(), false);
            IsChecklistModalOpen = false;
            StatusMessage = $"✔ Updated clearance checklist for order {SelectedOrder.OrderId}.";
            await LoadOrdersAsync();
        }

        private async Task GrantFinalClearanceAsync()
        {
            if (SelectedOrder == null) return;
            await _suspensionService.UpdateClearanceStatusAsync(SelectedOrder.Id, true, true, true, ChecklistNotes.Trim(), true);
            IsChecklistModalOpen = false;
            StatusMessage = $"✔ Final clearance granted & student {SelectedOrder.StudentName} readmitted to class.";
            await LoadOrdersAsync();
        }

        private void PrintNotice(SuspensionOrder order)
        {
            StatusMessage = $"🖨️ Official Suspension Notice for {order.StudentName} ({order.OrderId}) sent to print queue.";
        }
    }
}