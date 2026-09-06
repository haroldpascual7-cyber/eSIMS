using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class IncidentListViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly IRuleEngineService _ruleEngineService;
        private readonly ICommunicationService _commService;
        private readonly IStudentService _studentService;
        private readonly Action<Incident>? _openInvestigationAction;

        private string _searchText = string.Empty;
        private string _selectedCategoryFilter = "All Categories";
        private IncidentStatus? _selectedStatusFilter;
        private IncidentSeverity? _selectedSeverityFilter;
        private RiskLevel? _selectedRiskFilter;
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private Incident? _selectedIncident;
        private bool _isLoading;

        public IncidentListViewModel(
            IIncidentService incidentService,
            Action<Incident?>? legacyOpenDetailAction = null,
            Incident? preselectedIncident = null,
            Action<Incident>? openInvestigationAction = null,
            IRuleEngineService? ruleEngineService = null,
            ICommunicationService? commService = null,
            IStudentService? studentService = null)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _ruleEngineService = ruleEngineService ?? new RuleEngineService(new AppDbContext());
            _commService = commService ?? new CommunicationService(new AppDbContext());
            _studentService = studentService ?? new StudentService(new AppDbContext());
            _openInvestigationAction = openInvestigationAction;
            _selectedIncident = preselectedIncident;

            Incidents = new ObservableCollection<Incident>();
            Categories = new ObservableCollection<string>(_ruleEngineService.GetStandardCategories());

            CategoryFilterOptions = new ObservableCollection<string> { "All Categories" };
            foreach (var cat in Categories) CategoryFilterOptions.Add(cat);

            SeverityFilterOptions = new ObservableCollection<string> { "All Severities", "Low", "Medium", "High", "Critical" };
            StatusFilterOptions = new ObservableCollection<string> { "All Statuses", "Reported", "UnderInvestigation", "ActionTaken", "Resolved", "Closed" };

            // Commands
            NewIncidentCommand = new RelayCommand(OpenCreateModal);
            EditIncidentCommand = new RelayCommand(param =>
            {
                var target = (param as Incident) ?? SelectedIncident;
                if (target != null) OpenEditModal(target);
            }, _ => SelectedIncident != null);

            ViewIncidentDetailsCommand = new RelayCommand(param =>
            {
                var target = (param as Incident) ?? SelectedIncident;
                if (target != null) OpenViewModal(target);
            });

            OpenInvestigationCommand = new RelayCommand(param =>
            {
                var target = (param as Incident) ?? SelectedIncident;
                if (target != null)
                {
                    IsIncidentModalOpen = false;
                    _openInvestigationAction?.Invoke(target);
                }
            });

            DeleteIncidentCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedIncident != null && CanDeleteIncident);
            RefreshCommand = new RelayCommand(async () => await LoadIncidentsAsync());
            ClearFiltersCommand = new RelayCommand(async () =>
            {
                SearchText = string.Empty;
                SelectedCategoryFilter = "All Categories";
                SelectedStatusFilter = null;
                SelectedSeverityFilter = null;
                SelectedRiskFilter = null;
                FilterStartDate = null;
                FilterEndDate = null;
                await LoadIncidentsAsync();
            });

            // Initialize Modal Commands & State (defined in IncidentListViewModel.Modal.cs)
            InitializeModalCommands();

            _ = LoadIncidentsAsync();
            _ = LoadEnrolledStudentsAsync();

            if (preselectedIncident != null)
            {
                OpenViewModal(preselectedIncident);
            }
        }

        // --- Data Collections ---
        public ObservableCollection<Incident> Incidents { get; }
        public ObservableCollection<string> Categories { get; }
        public ObservableCollection<string> CategoryFilterOptions { get; }
        public ObservableCollection<string> SeverityFilterOptions { get; }
        public ObservableCollection<string> StatusFilterOptions { get; }

        public bool CanManageIncidents => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanDeleteIncident => SessionManager.HasPermission("System Administrator,Discipline Officer");
        public bool CanOpenInvestigation => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _ = LoadIncidentsAsync(); }
        }

        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set { if (SetProperty(ref _selectedCategoryFilter, value)) _ = LoadIncidentsAsync(); }
        }

        public IncidentStatus? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { if (SetProperty(ref _selectedStatusFilter, value)) _ = LoadIncidentsAsync(); }
        }

        public IncidentSeverity? SelectedSeverityFilter
        {
            get => _selectedSeverityFilter;
            set { if (SetProperty(ref _selectedSeverityFilter, value)) _ = LoadIncidentsAsync(); }
        }

        public RiskLevel? SelectedRiskFilter
        {
            get => _selectedRiskFilter;
            set { if (SetProperty(ref _selectedRiskFilter, value)) _ = LoadIncidentsAsync(); }
        }

        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set { if (SetProperty(ref _filterStartDate, value)) _ = LoadIncidentsAsync(); }
        }

        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set { if (SetProperty(ref _filterEndDate, value)) _ = LoadIncidentsAsync(); }
        }

        public Incident? SelectedIncident
        {
            get => _selectedIncident;
            set => SetProperty(ref _selectedIncident, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Commands
        public ICommand NewIncidentCommand { get; }
        public ICommand EditIncidentCommand { get; }
        public ICommand ViewIncidentDetailsCommand { get; }
        public ICommand OpenInvestigationCommand { get; }
        public ICommand DeleteIncidentCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public async Task LoadIncidentsAsync()
        {
            IsLoading = true;
            try
            {
                var items = await _incidentService.SearchIncidentsAsync(SearchText);

                if (!string.IsNullOrWhiteSpace(SelectedCategoryFilter) && !SelectedCategoryFilter.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
                {
                    items = items.Where(i => i.Category == SelectedCategoryFilter);
                }

                if (SelectedStatusFilter.HasValue)
                {
                    items = items.Where(i => i.Status == SelectedStatusFilter.Value);
                }

                if (SelectedSeverityFilter.HasValue)
                {
                    items = items.Where(i => i.Severity == SelectedSeverityFilter.Value);
                }

                if (FilterStartDate.HasValue)
                {
                    items = items.Where(i => i.IncidentDate.Date >= FilterStartDate.Value.Date);
                }

                if (FilterEndDate.HasValue)
                {
                    DateTime endLimit = FilterEndDate.Value.Date.AddDays(1).AddTicks(-1);
                    items = items.Where(i => i.IncidentDate <= endLimit);
                }

                // Scoped Teacher & Staff access
                if (SessionManager.Role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) ||
                    SessionManager.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    var userAccount = SessionManager.Username;
                    var userFullName = SessionManager.FullName;
                    items = items.Where(i =>
                        (!string.IsNullOrWhiteSpace(i.CreatedBy) && i.CreatedBy.Equals(userAccount, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(i.ReportingStaffTeacher) &&
                         (i.ReportingStaffTeacher.Equals(userAccount, StringComparison.OrdinalIgnoreCase) ||
                          i.ReportingStaffTeacher.Equals(userFullName, StringComparison.OrdinalIgnoreCase))));
                }

                Incidents.Clear();
                foreach (var item in items)
                {
                    Incidents.Add(item);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedIncident == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete incident '{SelectedIncident.IncidentNumber}' ({SelectedIncident.Title})?",
                "Confirm Incident Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _incidentService.DeleteIncidentAsync(SelectedIncident);
                SelectedIncident = null;
                await LoadIncidentsAsync();
            }
        }
    }
}
