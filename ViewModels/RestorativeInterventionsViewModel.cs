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
    public class RestorativeInterventionsViewModel : ObservableObject
    {
        private readonly IRestorativeService _restorativeService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onBackToOverview;

        private string _searchText = string.Empty;
        private string _selectedTypeFilter = "All Types";
        private string _selectedStatusFilter = "All Statuses";
        private string _statusMessage = string.Empty;

        // KPIs
        private int _activePlansCount;
        private int _totalServiceHoursCount;
        private int _pendingCounselingSessionsCount;
        private int _clearedCount;

        // Modal 1: Add Plan State
        private bool _isAddModalOpen;
        private string _addStudentName = "Michael Brown";
        private string _addStudentId = "STD-1042";
        private string _addIncidentNumber = "INC-2026-00004";
        private string _addPlanType = "Community Service";
        private string _addAssignedStaff = "Facilities & Guidance";
        private int _addTargetMetric = 10;
        private DateTime _addStartDate = DateTime.Now;
        private DateTime _addDeadlineDate = DateTime.Now.AddDays(14);
        private string _addObjectives = string.Empty;
        private string _addErrorMessage = string.Empty;

        // Modal 2: Log Progress State
        private bool _isProgressModalOpen;
        private RestorativePlan? _selectedPlan;
        private int _progressUnits = 2;
        private DateTime _progressLogDate = DateTime.Now;
        private string _progressStaffName = "Guidance Office";
        private string _progressNotes = string.Empty;
        private bool _progressMarkCompleted;
        private string _progressErrorMessage = string.Empty;

        public RestorativeInterventionsViewModel(
            IRestorativeService restorativeService,
            IIncidentService incidentService,
            Action onBackToOverview)
        {
            _restorativeService = restorativeService ?? throw new ArgumentNullException(nameof(restorativeService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _onBackToOverview = onBackToOverview ?? throw new ArgumentNullException(nameof(onBackToOverview));

            InterventionPlans = new ObservableCollection<RestorativePlan>();
            AvailableIncidents = new ObservableCollection<string>();
            AvailableStudents = new ObservableCollection<string>();

            TypeFilterOptions = new ObservableCollection<string>
            {
                "All Types",
                "Community Service",
                "Individual Counseling",
                "Peer Mediation",
                "Behavioral Contract"
            };

            StatusFilterOptions = new ObservableCollection<string>
            {
                "All Statuses",
                "Active / In-Progress",
                "Completed",
                "Overdue"
            };

            PlanTypeOptions = new ObservableCollection<string>
            {
                "Community Service",
                "Individual Counseling",
                "Peer Mediation",
                "Behavioral Contract",
                "Anger Management Workshop"
            };

            BackToOverviewCommand = new RelayCommand(() => _onBackToOverview());
            OpenAddModalCommand = new RelayCommand(OpenAddModal);
            CloseAddModalCommand = new RelayCommand(CloseAddModal);
            SaveNewPlanCommand = new RelayCommand(async () => await SaveNewPlanAsync());

            OpenProgressModalCommand = new RelayCommand(param =>
            {
                if (param is RestorativePlan plan) OpenProgressModal(plan);
            });
            CloseProgressModalCommand = new RelayCommand(CloseProgressModal);
            SaveProgressCommand = new RelayCommand(async () => await SaveProgressAsync());
            PrintCertificateCommand = new RelayCommand(param =>
            {
                if (param is RestorativePlan plan) PrintCertificate(plan);
            });

            _ = InitializeDataAsync();
        }

        public ObservableCollection<RestorativePlan> InterventionPlans { get; }
        public ObservableCollection<string> AvailableIncidents { get; }
        public ObservableCollection<string> AvailableStudents { get; }

        public ObservableCollection<string> TypeFilterOptions { get; }
        public ObservableCollection<string> StatusFilterOptions { get; }
        public ObservableCollection<string> PlanTypeOptions { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = FilterPlansAsync();
                }
            }
        }

        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    _ = FilterPlansAsync();
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
                    _ = FilterPlansAsync();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // KPIs
        public int ActivePlansCount
        {
            get => _activePlansCount;
            set => SetProperty(ref _activePlansCount, value);
        }

        public int TotalServiceHoursCount
        {
            get => _totalServiceHoursCount;
            set => SetProperty(ref _totalServiceHoursCount, value);
        }

        public int PendingCounselingSessionsCount
        {
            get => _pendingCounselingSessionsCount;
            set => SetProperty(ref _pendingCounselingSessionsCount, value);
        }

        public int ClearedCount
        {
            get => _clearedCount;
            set => SetProperty(ref _clearedCount, value);
        }

        // Modal 1 Properties
        public bool IsAddModalOpen
        {
            get => _isAddModalOpen;
            set => SetProperty(ref _isAddModalOpen, value);
        }

        public string AddStudentName
        {
            get => _addStudentName;
            set => SetProperty(ref _addStudentName, value);
        }

        public string AddStudentId
        {
            get => _addStudentId;
            set => SetProperty(ref _addStudentId, value);
        }

        public string AddIncidentNumber
        {
            get => _addIncidentNumber;
            set => SetProperty(ref _addIncidentNumber, value);
        }

        public string AddPlanType
        {
            get => _addPlanType;
            set => SetProperty(ref _addPlanType, value);
        }

        public string AddAssignedStaff
        {
            get => _addAssignedStaff;
            set => SetProperty(ref _addAssignedStaff, value);
        }

        public int AddTargetMetric
        {
            get => _addTargetMetric;
            set => SetProperty(ref _addTargetMetric, value);
        }

        public DateTime AddStartDate
        {
            get => _addStartDate;
            set => SetProperty(ref _addStartDate, value);
        }

        public DateTime AddDeadlineDate
        {
            get => _addDeadlineDate;
            set => SetProperty(ref _addDeadlineDate, value);
        }

        public string AddObjectives
        {
            get => _addObjectives;
            set => SetProperty(ref _addObjectives, value);
        }

        public string AddErrorMessage
        {
            get => _addErrorMessage;
            set => SetProperty(ref _addErrorMessage, value);
        }

        // Modal 2 Properties
        public bool IsProgressModalOpen
        {
            get => _isProgressModalOpen;
            set => SetProperty(ref _isProgressModalOpen, value);
        }

        public RestorativePlan? SelectedPlan
        {
            get => _selectedPlan;
            set => SetProperty(ref _selectedPlan, value);
        }

        public int ProgressUnits
        {
            get => _progressUnits;
            set => SetProperty(ref _progressUnits, value);
        }

        public DateTime ProgressLogDate
        {
            get => _progressLogDate;
            set => SetProperty(ref _progressLogDate, value);
        }

        public string ProgressStaffName
        {
            get => _progressStaffName;
            set => SetProperty(ref _progressStaffName, value);
        }

        public string ProgressNotes
        {
            get => _progressNotes;
            set => SetProperty(ref _progressNotes, value);
        }

        public bool ProgressMarkCompleted
        {
            get => _progressMarkCompleted;
            set => SetProperty(ref _progressMarkCompleted, value);
        }

        public string ProgressErrorMessage
        {
            get => _progressErrorMessage;
            set => SetProperty(ref _progressErrorMessage, value);
        }

        // Commands
        public ICommand BackToOverviewCommand { get; }
        public ICommand OpenAddModalCommand { get; }
        public ICommand CloseAddModalCommand { get; }
        public ICommand SaveNewPlanCommand { get; }
        public ICommand OpenProgressModalCommand { get; }
        public ICommand CloseProgressModalCommand { get; }
        public ICommand SaveProgressCommand { get; }
        public ICommand PrintCertificateCommand { get; }

        private async Task InitializeDataAsync()
        {
            var incidents = await _incidentService.GetAllIncidentsAsync();
            AvailableIncidents.Clear();
            foreach (var inc in incidents) AvailableIncidents.Add(inc.IncidentNumber);
            if (AvailableIncidents.Any()) AddIncidentNumber = AvailableIncidents.First();

            var students = await _incidentService.GetAllStudentsAsync();
            AvailableStudents.Clear();
            foreach (var st in students) AvailableStudents.Add($"{st.FullName} ({st.StudentNumber})");

            await LoadPlansAsync();
        }

        private async Task LoadPlansAsync()
        {
            var plans = await _restorativeService.GetAllInterventionPlansAsync();
            UpdatePlansCollection(plans);
        }

        private async Task FilterPlansAsync()
        {
            var plans = await _restorativeService.SearchAndFilterPlansAsync(SearchText, SelectedTypeFilter, SelectedStatusFilter);
            UpdatePlansCollection(plans);
        }

        private void UpdatePlansCollection(IEnumerable<RestorativePlan> plans)
        {
            InterventionPlans.Clear();
            var list = plans.ToList();
            foreach (var p in list) InterventionPlans.Add(p);

            ActivePlansCount = list.Count(p => p.Status == "Active" || p.Status == "In-Progress");
            TotalServiceHoursCount = list.Where(p => p.PlanType == "Community Service").Sum(p => p.CompletedMetric);
            PendingCounselingSessionsCount = list.Where(p => p.PlanType.Contains("Counseling") && p.Status != "Completed").Sum(p => Math.Max(0, p.TargetMetric - p.CompletedMetric));
            ClearedCount = list.Count(p => p.IsCleared || p.Status == "Completed");
        }

        private void OpenAddModal()
        {
            AddErrorMessage = string.Empty;
            AddStudentName = "Michael Brown";
            AddStudentId = "STD-1042";
            AddPlanType = "Community Service";
            AddAssignedStaff = "Facilities & Guidance";
            AddTargetMetric = 10;
            AddStartDate = DateTime.Now;
            AddDeadlineDate = DateTime.Now.AddDays(14);
            AddObjectives = string.Empty;

            IsAddModalOpen = true;
        }

        private void CloseAddModal() => IsAddModalOpen = false;

        private async Task SaveNewPlanAsync()
        {
            AddErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(AddStudentName))
            {
                AddErrorMessage = "Please enter the student's name.";
                return;
            }

            if (AddTargetMetric <= 0)
            {
                AddErrorMessage = "Target metric (hours/sessions) must be greater than 0.";
                return;
            }

            if (string.IsNullOrWhiteSpace(AddObjectives))
            {
                AddErrorMessage = "Please outline intervention objectives & tasks.";
                return;
            }

            var plan = new RestorativePlan
            {
                PlanId = await _restorativeService.GeneratePlanIdAsync(),
                IncidentNumber = string.IsNullOrWhiteSpace(AddIncidentNumber) ? "INC-2026-00004" : AddIncidentNumber,
                StudentName = AddStudentName.Trim(),
                StudentIdNumber = string.IsNullOrWhiteSpace(AddStudentId) ? "STD-1042" : AddStudentId.Trim(),
                PlanType = AddPlanType,
                AssignedStaff = AddAssignedStaff.Trim(),
                TargetMetric = AddTargetMetric,
                CompletedMetric = 0,
                StartDate = AddStartDate,
                DeadlineDate = AddDeadlineDate,
                Objectives = AddObjectives.Trim(),
                Status = "Active",
                IsCleared = false
            };

            await _restorativeService.AddInterventionPlanAsync(plan);
            IsAddModalOpen = false;
            StatusMessage = $"✔ Restorative Intervention Plan {plan.PlanId} created successfully.";
            await LoadPlansAsync();
        }

        private void OpenProgressModal(RestorativePlan plan)
        {
            SelectedPlan = plan;
            ProgressErrorMessage = string.Empty;
            ProgressUnits = 2;
            ProgressLogDate = DateTime.Now;
            ProgressStaffName = "Guidance Office";
            ProgressNotes = string.Empty;
            ProgressMarkCompleted = false;

            IsProgressModalOpen = true;
        }

        private void CloseProgressModal() => IsProgressModalOpen = false;

        private async Task SaveProgressAsync()
        {
            ProgressErrorMessage = string.Empty;

            if (SelectedPlan == null) return;

            if (ProgressUnits <= 0)
            {
                ProgressErrorMessage = "Logged units must be at least 1.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ProgressNotes))
            {
                ProgressErrorMessage = "Please enter session notes or student reflection.";
                return;
            }

            await _restorativeService.LogProgressAsync(SelectedPlan.Id, ProgressUnits, ProgressNotes.Trim(), ProgressStaffName.Trim(), ProgressMarkCompleted);
            IsProgressModalOpen = false;
            StatusMessage = $"✔ Logged +{ProgressUnits} {SelectedPlan.MetricUnitLabel} for plan {SelectedPlan.PlanId}.";
            await LoadPlansAsync();
        }

        private void PrintCertificate(RestorativePlan plan)
        {
            if (!plan.IsCleared && plan.Status != "Completed")
            {
                StatusMessage = $"⚠ Plan {plan.PlanId} is not yet completed. Complete required units before printing Clearance Certificate.";
                return;
            }

            StatusMessage = $"🖨️ Official Rehabilitation & Clearance Certificate for {plan.StudentName} ({plan.PlanId}) generated.";
        }
    }
}