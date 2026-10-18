using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ConsequenceTrackingViewModel : ObservableObject
    {
        private readonly IConsequenceService _consequenceService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onBackToOverview;

        private string _searchText = string.Empty;
        private string _selectedCategoryFilter = "All";
        private string _selectedStatusFilter = "All";
        private string _statusMessage = string.Empty;

        // KPI Counts
        private int _totalSanctionsCount;
        private int _pendingConferencesCount;
        private int _completedCount;
        private int _nonCompliantCount;

        // Modal Dialog State
        private bool _isModalOpen;
        private string _modalStudentName = string.Empty;
        private string _modalStudentId = "STD-1001";
        private string _modalIncidentNumber = "INC-2026-00001";
        private string _modalConsequenceType = "Verbal Warning";
        private string _modalOffenseLevel = "1st Offense";
        private string _modalIssuedBy = "admin";
        private DateTime _modalDateIssued = DateTime.Now;
        private DateTime _modalDueDate = DateTime.Now.AddDays(7);
        private bool _modalParentNotified = true;
        private string _modalSanctionTerms = string.Empty;
        private string _modalErrorMessage = string.Empty;

        public ConsequenceTrackingViewModel(
            IConsequenceService consequenceService,
            IIncidentService incidentService,
            Action onBackToOverview)
        {
            _consequenceService = consequenceService ?? throw new ArgumentNullException(nameof(consequenceService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _onBackToOverview = onBackToOverview ?? throw new ArgumentNullException(nameof(onBackToOverview));

            ConsequenceLogs = new ObservableCollection<ConsequenceLog>();
            AvailableIncidents = new ObservableCollection<string>();
            AvailableStudents = new ObservableCollection<string>();

            CategoryFilterOptions = new ObservableCollection<string>
            {
                "All",
                "Verbal Warning",
                "Written Reprimand",
                "Parent Conference",
                "Behavioral Contract"
            };

            StatusFilterOptions = new ObservableCollection<string>
            {
                "All",
                "Pending",
                "Scheduled",
                "Completed",
                "Overdue"
            };

            ConsequenceTypeOptions = new ObservableCollection<string>
            {
                "Verbal Warning",
                "Written Reprimand",
                "Parent Conference",
                "Detention",
                "Behavioral Contract"
            };

            OffenseLevelOptions = new ObservableCollection<string>
            {
                "1st Offense",
                "2nd Offense",
                "3rd Offense / Critical"
            };

            BackToOverviewCommand = new RelayCommand(() => _onBackToOverview());
            OpenNewLogModalCommand = new RelayCommand(OpenNewLogModal);
            CloseModalCommand = new RelayCommand(CloseModal);
            SaveNewLogCommand = new RelayCommand(async () => await SaveNewLogAsync());
            MarkCompletedCommand = new RelayCommand(async param =>
            {
                if (param is ConsequenceLog log) await MarkAsCompletedAsync(log);
            });
            PrintNoticeSlipCommand = new RelayCommand(param =>
            {
                if (param is ConsequenceLog log) PrintNoticeSlip(log);
            });

            _ = InitializeDataAsync();
        }

        public ObservableCollection<ConsequenceLog> ConsequenceLogs { get; }
        public ObservableCollection<string> AvailableIncidents { get; }
        public ObservableCollection<string> AvailableStudents { get; }

        public ObservableCollection<string> CategoryFilterOptions { get; }
        public ObservableCollection<string> StatusFilterOptions { get; }
        public ObservableCollection<string> ConsequenceTypeOptions { get; }
        public ObservableCollection<string> OffenseLevelOptions { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = FilterLogsAsync();
                }
            }
        }

        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedCategoryFilter, value))
                {
                    _ = FilterLogsAsync();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    _ = FilterLogsAsync();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // KPIs
        public int TotalSanctionsCount
        {
            get => _totalSanctionsCount;
            set => SetProperty(ref _totalSanctionsCount, value);
        }

        public int PendingConferencesCount
        {
            get => _pendingConferencesCount;
            set => SetProperty(ref _pendingConferencesCount, value);
        }

        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }

        public int NonCompliantCount
        {
            get => _nonCompliantCount;
            set => SetProperty(ref _nonCompliantCount, value);
        }

        // Modal Form Properties
        public bool IsModalOpen
        {
            get => _isModalOpen;
            set => SetProperty(ref _isModalOpen, value);
        }

        public string ModalStudentName
        {
            get => _modalStudentName;
            set => SetProperty(ref _modalStudentName, value);
        }

        public string ModalStudentId
        {
            get => _modalStudentId;
            set => SetProperty(ref _modalStudentId, value);
        }

        public string ModalIncidentNumber
        {
            get => _modalIncidentNumber;
            set => SetProperty(ref _modalIncidentNumber, value);
        }

        public string ModalConsequenceType
        {
            get => _modalConsequenceType;
            set => SetProperty(ref _modalConsequenceType, value);
        }

        public string ModalOffenseLevel
        {
            get => _modalOffenseLevel;
            set => SetProperty(ref _modalOffenseLevel, value);
        }

        public string ModalIssuedBy
        {
            get => _modalIssuedBy;
            set => SetProperty(ref _modalIssuedBy, value);
        }

        public DateTime ModalDateIssued
        {
            get => _modalDateIssued;
            set => SetProperty(ref _modalDateIssued, value);
        }

        public DateTime ModalDueDate
        {
            get => _modalDueDate;
            set => SetProperty(ref _modalDueDate, value);
        }

        public bool ModalParentNotified
        {
            get => _modalParentNotified;
            set => SetProperty(ref _modalParentNotified, value);
        }

        public string ModalSanctionTerms
        {
            get => _modalSanctionTerms;
            set => SetProperty(ref _modalSanctionTerms, value);
        }

        public string ModalErrorMessage
        {
            get => _modalErrorMessage;
            set => SetProperty(ref _modalErrorMessage, value);
        }

        // Commands
        public ICommand BackToOverviewCommand { get; }
        public ICommand OpenNewLogModalCommand { get; }
        public ICommand CloseModalCommand { get; }
        public ICommand SaveNewLogCommand { get; }
        public ICommand MarkCompletedCommand { get; }
        public ICommand PrintNoticeSlipCommand { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                IsLoading = true;
                var incidents = await _incidentService.GetAllIncidentsAsync();
                AvailableIncidents.Clear();
                foreach (var inc in incidents)
                {
                    AvailableIncidents.Add(inc.IncidentNumber);
                }
                if (AvailableIncidents.Any()) ModalIncidentNumber = AvailableIncidents.First();

                var students = await _incidentService.GetAllStudentsAsync();
                AvailableStudents.Clear();
                foreach (var st in students)
                {
                    AvailableStudents.Add($"{st.FullName} ({st.StudentNumber})");
                }
                if (AvailableStudents.Any()) ModalStudentName = "Juan Dela Cruz";

                await LoadLogsAsync();
            }
            finally
            {
                await Task.Delay(100);
                IsLoading = false;
            }
        }

        private async Task LoadLogsAsync()
        {
            var logs = await _consequenceService.GetAllConsequenceLogsAsync();
            UpdateLogsCollection(logs);
        }

        private async Task FilterLogsAsync()
        {
            var logs = await _consequenceService.SearchAndFilterLogsAsync(SearchText, SelectedCategoryFilter, SelectedStatusFilter);
            UpdateLogsCollection(logs);
        }

        private void UpdateLogsCollection(System.Collections.Generic.IEnumerable<ConsequenceLog> logs)
        {
            ConsequenceLogs.Clear();
            var list = logs.ToList();
            foreach (var l in list) ConsequenceLogs.Add(l);

            TotalSanctionsCount = list.Count;
            PendingConferencesCount = list.Count(c => c.ConsequenceType == "Parent Conference" && c.Status != "Completed");
            CompletedCount = list.Count(c => c.Status == "Completed");
            NonCompliantCount = list.Count(c => c.Status == "Overdue" || c.Status == "Overdue/No-Show");
        }

        private void OpenNewLogModal()
        {
            ModalErrorMessage = string.Empty;
            ModalStudentName = "Juan Dela Cruz";
            ModalStudentId = "STD-1042";
            ModalConsequenceType = "Verbal Warning";
            ModalOffenseLevel = "1st Offense";
            ModalIssuedBy = "admin";
            ModalDateIssued = DateTime.Now;
            ModalDueDate = DateTime.Now.AddDays(7);
            ModalParentNotified = true;
            ModalSanctionTerms = string.Empty;

            IsModalOpen = true;
        }

        private void CloseModal()
        {
            IsModalOpen = false;
        }

        private async Task SaveNewLogAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalStudentName))
            {
                ModalErrorMessage = "Please select or enter student name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ModalSanctionTerms))
            {
                ModalErrorMessage = "Please provide sanction terms and details.";
                return;
            }

            var newLog = new ConsequenceLog
            {
                LogId = await _consequenceService.GenerateLogIdAsync(),
                IncidentNumber = string.IsNullOrWhiteSpace(ModalIncidentNumber) ? "INC-2026-00004" : ModalIncidentNumber,
                StudentName = ModalStudentName.Contains("(") ? ModalStudentName.Split('(')[0].Trim() : ModalStudentName.Trim(),
                StudentIdNumber = string.IsNullOrWhiteSpace(ModalStudentId) ? "STD-1042" : ModalStudentId.Trim(),
                ConsequenceType = ModalConsequenceType,
                OffenseLevel = ModalOffenseLevel,
                DateIssued = ModalDateIssued,
                DueDate = ModalDueDate,
                IssuedBy = ModalIssuedBy,
                ParentNotified = ModalParentNotified,
                ParentSigned = false,
                Status = "Scheduled",
                SanctionTerms = ModalSanctionTerms.Trim()
            };

            await _consequenceService.AddConsequenceLogAsync(newLog);
            IsModalOpen = false;
            StatusMessage = $"✔ Consequence log {newLog.LogId} recorded successfully.";
            await LoadLogsAsync();
        }

        private async Task MarkAsCompletedAsync(ConsequenceLog log)
        {
            await _consequenceService.UpdateConsequenceStatusAsync(log.Id, "Completed");
            StatusMessage = $"✔ Consequence {log.LogId} marked as Completed.";
            await LoadLogsAsync();
        }

        private void PrintNoticeSlip(ConsequenceLog log)
        {
            StatusMessage = $"🖨️ Official Notice Slip for {log.LogId} ({log.StudentName}) sent to printer queue.";
        }
    }
}