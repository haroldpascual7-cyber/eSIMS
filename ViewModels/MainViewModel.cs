using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly IThemeService _themeService;
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;
        private readonly IRuleEngineService _ruleEngineService;
        private readonly IInvestigationService _investigationService;
        private readonly IConsequenceService _consequenceService;
        private readonly IRestorativeService _restorativeService;
        private readonly ISuspensionService _suspensionService;
        private readonly IProbationService _probationService;
        private readonly IIncidentClassificationService _classificationService;
        private readonly IBehavioralTranscriptService _transcriptService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IActionPlanService _planService;
        private readonly ILetterTemplateService _templateService;
        private readonly IPdfExportService _pdfService;
        private readonly ICommunicationService _commService;
        private readonly IAcknowledgmentService _ackService;
        private readonly IHeatmapService _heatmapService;
        private readonly IRecidivismService _recidivismService;
        private readonly IAuditService _auditService;
        private readonly IIntegrityAuditService _integrityService;
        private readonly ISecurityAccessLogService _securityLogService;
        private readonly IStudentService _studentService;
        private readonly IAnecdotalService _anecdotalService;

        private object _currentViewModel;
        private string _activeSection = "Login";
        private bool _isAuthenticated;

        public MainViewModel(
            IIncidentService incidentService,
            IThemeService themeService,
            IAuthService authService,
            ISettingsService settingsService,
            IRuleEngineService ruleEngineService,
            IInvestigationService investigationService,
            IConsequenceService consequenceService,
            IRestorativeService restorativeService,
            ISuspensionService suspensionService,
            IProbationService probationService,
            IIncidentClassificationService classificationService,
            IBehavioralTranscriptService transcriptService,
            IAnalyticsService? analyticsService = null,
            IActionPlanService? planService = null,
            ILetterTemplateService? templateService = null,
            IPdfExportService? pdfService = null,
            ICommunicationService? commService = null,
            IAcknowledgmentService? ackService = null,
            IHeatmapService? heatmapService = null,
            IRecidivismService? recidivismService = null,
            IAuditService? auditService = null,
            IIntegrityAuditService? integrityService = null,
            ISecurityAccessLogService? securityLogService = null,
            IStudentService? studentService = null,
            IAnecdotalService? anecdotalService = null)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _ruleEngineService = ruleEngineService ?? throw new ArgumentNullException(nameof(ruleEngineService));
            _investigationService = investigationService ?? throw new ArgumentNullException(nameof(investigationService));
            _consequenceService = consequenceService ?? throw new ArgumentNullException(nameof(consequenceService));
            _restorativeService = restorativeService ?? throw new ArgumentNullException(nameof(restorativeService));
            _suspensionService = suspensionService ?? throw new ArgumentNullException(nameof(suspensionService));
            _probationService = probationService ?? throw new ArgumentNullException(nameof(probationService));
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
            _analyticsService = analyticsService ?? new AnalyticsService(new AppDbContext());
            _planService = planService ?? new ActionPlanService(new AppDbContext());
            _templateService = templateService ?? new LetterTemplateService(new AppDbContext());
            _pdfService = pdfService ?? new PdfExportService();
            _commService = commService ?? new CommunicationService(new AppDbContext());
            _ackService = ackService ?? new AcknowledgmentService(new AppDbContext());
            _heatmapService = heatmapService ?? new HeatmapService(new AppDbContext());
            _recidivismService = recidivismService ?? new RecidivismService(new AppDbContext());
            _auditService = auditService ?? new AuditService(new AppDbContext());
            _integrityService = integrityService ?? new IntegrityAuditService(new AppDbContext());
            _securityLogService = securityLogService ?? new SecurityAccessLogService(new AppDbContext());
            _studentService = studentService ?? new StudentService(new AppDbContext());
            _anecdotalService = anecdotalService ?? new AnecdotalService();

            // Primary 8 Workspaces Commands
            NavigateDashboardCommand = new RelayCommand(ShowOperationalDashboard);
            NavigateStudentsCommand = new RelayCommand(ShowStudents);
            NavigateIncidentsCommand = new RelayCommand(() => ShowIncidents());
            NavigateInvestigationsCommand = new RelayCommand(ShowInvestigations);
            NavigateBehaviorCommand = new RelayCommand(() => ShowBehaviorWorkspace());
            NavigateParentNoticesCommand = new RelayCommand(ShowParentNotices);
            NavigateReportsAnalyticsCommand = new RelayCommand(ShowReportsAnalytics);
            NavigateSettingsCommand = new RelayCommand(() => ShowSettingsWorkspace());

            // Legacy & Shortcut Aliases
            NavigateTabletDashboardCommand = new RelayCommand(ShowDashboard);
            NavigateReportNewIncidentCommand = new RelayCommand(() => ShowIncidents());
            NavigateDisciplinaryActionsCommand = new RelayCommand(ShowInvestigations);
            NavigateRiskRulesCommand = new RelayCommand(() => ShowSettingsWorkspace(3));
            NavigateStudentProfilesCommand = new RelayCommand(() => ShowBehaviorWorkspace());
            NavigateAuditTrailCommand = new RelayCommand(() => ShowSettingsWorkspace(5));
            NavigateUserManagementCommand = new RelayCommand(() => ShowSettingsWorkspace(0));
            NavigateSystemSettingsCommand = new RelayCommand(() => ShowSettingsWorkspace(4));

            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            LogoutCommand = new RelayCommand(Logout);
            InitializeNotificationSystem();
            InitializeBrandingSupport();
            ViewNotificationsCommand = OpenNotificationModalCommand;

            _currentViewModel = new LoginViewModel(_authService, _themeService, _settingsService, OnLoginSuccess);
            _isAuthenticated = false;
        }

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (SetProperty(ref _currentViewModel, value))
                {
                    NotifyActiveStates();
                }
            }
        }

        public string ActiveSection
        {
            get => _activeSection;
            set
            {
                if (SetProperty(ref _activeSection, value))
                {
                    NotifyActiveStates();
                }
            }
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetProperty(ref _isAuthenticated, value);
        }

        public User? CurrentUser => _authService.CurrentUser;
        public string CurrentUserName => CurrentUser?.Username ?? string.Empty;
        public string CurrentUserRole => CurrentUser?.Role ?? string.Empty;

        // Active State Checkers for Primary Navigation Tabs
        public bool IsDashboardActive => ActiveSection == "Dashboard";
        public bool IsDashboardOverviewActive => ActiveSection == "Dashboard Overview";
        public bool IsStudentsActive => ActiveSection == "Students";
        public bool IsIncidentsActive => ActiveSection == "Incidents";
        public bool IsInvestigationsActive => ActiveSection == "Investigations" || ActiveSection == "DisciplinaryActions";
        public bool IsBehaviorActive => ActiveSection == "Behavior";
        public bool IsParentNoticesActive => ActiveSection == "ParentNotices";
        public bool IsReportsAnalyticsActive => ActiveSection == "ReportsAnalytics";
        public bool IsSettingsActive => ActiveSection == "Settings";
        public bool IsTabletDashboardActive => IsDashboardActive;

        public string ActiveSectionTitle => ActiveSection switch
        {
            "Dashboard" => "System Dashboard",
            "Dashboard Overview" => "Operational Dashboard & Telemetry",
            "Students" => "Students Directory & Records",
            "Incidents" => "Incidents Records & Intelligence Log",
            "Investigations" => "Case Investigation Workspace",
            "DisciplinaryActions" => "Suspension & Probation Tracking",
            "Behavior" => "Student Behavioral Management",
            "ParentNotices" => "Parent Notices & Official Disclosures",
            "ReportsAnalytics" => "Safety Reports & Safety Analytics",
            "Settings" => "System Administration & Settings",
            _ => ActiveSection
        };

        public string UserInitials
        {
            get
            {
                var name = SessionManager.FullName;
                if (string.IsNullOrWhiteSpace(name)) name = SessionManager.Username;
                if (string.IsNullOrWhiteSpace(name)) return "U";
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
            }
        }

        // Role-Based Navigation Permissions
        public bool CanAccessStudents => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessIncidents => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessInvestigations => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");
        public bool CanAccessBehavior => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessParentNotices => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");
        public bool CanAccessReports => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessSettings => SessionManager.HasPermission("System Administrator,Discipline Officer");
        public bool IsNotificationBadgeVisible => true;

        // Primary Navigation Commands
        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateStudentsCommand { get; }
        public ICommand NavigateIncidentsCommand { get; }
        public ICommand NavigateInvestigationsCommand { get; }
        public ICommand NavigateBehaviorCommand { get; }
        public ICommand NavigateParentNoticesCommand { get; }
        public ICommand NavigateReportsAnalyticsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }

        // Aliases
        public ICommand NavigateTabletDashboardCommand { get; }
        public ICommand NavigateReportNewIncidentCommand { get; }
        public ICommand NavigateDisciplinaryActionsCommand { get; }
        public ICommand NavigateRiskRulesCommand { get; }
        public ICommand NavigateStudentProfilesCommand { get; }
        public ICommand NavigateAuditTrailCommand { get; }
        public ICommand NavigateUserManagementCommand { get; }
        public ICommand NavigateSystemSettingsCommand { get; }

        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ViewNotificationsCommand { get; }

        private void OnLoginSuccess()
        {
            IsAuthenticated = true;
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(CurrentUserRole));
            OnPropertyChanged(nameof(CanAccessStudents));
            OnPropertyChanged(nameof(CanAccessIncidents));
            OnPropertyChanged(nameof(CanAccessInvestigations));
            OnPropertyChanged(nameof(CanAccessBehavior));
            OnPropertyChanged(nameof(CanAccessParentNotices));
            OnPropertyChanged(nameof(CanAccessReports));
            OnPropertyChanged(nameof(CanAccessSettings));
            OnPropertyChanged(nameof(IsNotificationBadgeVisible));
            OnPropertyChanged(nameof(UserInitials));
            OnPropertyChanged(nameof(UnreadNotificationCount));
            OnPropertyChanged(nameof(HasUnreadNotifications));

            _ = LoadNotificationsAsync();
            ShowDashboard();
        }

        public void Logout()
        {
            _authService.Logout();
            IsAuthenticated = false;
            ActiveSection = "Login";
            CurrentViewModel = new LoginViewModel(_authService, _themeService, _settingsService, OnLoginSuccess);
        }

        public void ToggleTheme()
        {
            _themeService.ToggleTheme();
        }

        // ==========================================
        // 8 CANONICAL WORKSPACE NAVIGATORS
        // ==========================================

        // 1. DASHBOARD
        public void ShowDashboard()
        {
            ActiveSection = "Dashboard";
            CurrentViewModel = new TabletDashboardViewModel(
                _incidentService,
                incident => ShowIncidentDetail(incident),
                () => ShowIncidents());
        }

        public void ShowTabletDashboard() => ShowDashboard();

        public void ShowOperationalDashboard()
        {
            ActiveSection = "Dashboard Overview";
            CurrentViewModel = new DashboardViewModel(
                _incidentService,
                incident => ShowIncidentDetail(incident),
                () => ShowIncidents(),
                () => ShowDashboard(),
                _settingsService);
        }

        // 2. STUDENTS
        public void ShowStudents()
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff", "Students Directory")) return;
            ActiveSection = "Students";
            CurrentViewModel = new StudentsViewModel(
                _studentService,
                _incidentService,
                studentName => ShowIncidents(createNew: true, forStudentName: studentName));
        }

        // 3. INCIDENTS
        public void ShowIncidents(
            Incident? preselectedIncident = null,
            bool createNew = false,
            string? forStudentName = null,
            string? statusFilter = null,
            string? severityFilter = null,
            string? searchKeyword = null)
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff", "Incidents Management")) return;
            ActiveSection = "Incidents";
            var vm = new IncidentListViewModel(
                _incidentService,
                incident => ShowIncidentDetail(incident),
                preselectedIncident,
                incident => ShowInvestigationWorkspace(incident),
                _ruleEngineService,
                _commService,
                _studentService);

            if (!string.IsNullOrWhiteSpace(statusFilter))
                vm.SelectedStatusFilter = statusFilter;
            if (!string.IsNullOrWhiteSpace(severityFilter))
                vm.SelectedSeverityFilter = severityFilter;
            if (!string.IsNullOrWhiteSpace(searchKeyword))
                vm.SearchText = searchKeyword;

            if (createNew)
            {
                if (!string.IsNullOrWhiteSpace(forStudentName))
                    vm.OpenCreateModalForStudent(forStudentName);
                else
                    vm.NewIncidentCommand.Execute(null);
            }

            CurrentViewModel = vm;
        }

        public void ShowIncidentDetail(Incident? incident)
        {
            if (incident != null)
                ShowIncidents(preselectedIncident: incident);
            else
                ShowIncidents();
        }

        // 4. INVESTIGATIONS
        public void ShowInvestigations()
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor", "Investigations")) return;
            ActiveSection = "Investigations";
            CurrentViewModel = new InvestigationListViewModel(
                _incidentService,
                _investigationService,
                incident => ShowInvestigationWorkspace(incident));
        }

        public void ShowInvestigationList(Incident? preselectedIncident = null)
        {
            if (preselectedIncident != null)
                ShowInvestigationWorkspace(preselectedIncident);
            else
                ShowInvestigations();
        }

        public void ShowInvestigationWorkspace(Incident incident)
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor", "Investigation Workspace")) return;
            ActiveSection = "Investigations";
            CurrentViewModel = new InvestigationDetailViewModel(
                _investigationService,
                _incidentService,
                incident,
                () => ShowInvestigations(),
                ShowDisciplinaryActions);
        }

        public void ShowStudentInvestigationInquiry(Incident incident)
        {
            ShowInvestigationWorkspace(incident);
        }

        // 5. BEHAVIOR
        public void ShowBehaviorWorkspace(Student? student = null)
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff", "Student Behavioral Profiles")) return;
            ActiveSection = "Behavior";
            var vm = new BehaviorWorkspaceViewModel(
                _transcriptService,
                _anecdotalService,
                _restorativeService,
                _planService,
                _pdfService,
                _studentService);

            if (student != null)
                vm.SelectStudentCommand.Execute(student);

            CurrentViewModel = vm;
        }

        public void ShowStudentProfiles() => ShowBehaviorWorkspace();
        public void ShowBehavioralTranscripts() => ShowBehaviorWorkspace();
        public void ShowConductAnalytics() => ShowBehaviorWorkspace();
        public void ShowIndividualActionPlans() => ShowBehaviorWorkspace();
        public void ShowAnecdotalRecords() => ShowBehaviorWorkspace();

        // 6. PARENT NOTICES
        public void ShowParentNotices()
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor", "Parent Notices")) return;
            ActiveSection = "ParentNotices";
            CurrentViewModel = new ParentNoticesViewModel(
                _ackService,
                _commService,
                _templateService,
                _studentService,
                _pdfService);
        }

        public void ShowLetterTemplates() => ShowParentNotices();
        public void ShowDispatch() => ShowParentNotices();
        public void ShowAcknowledgmentTracking() => ShowParentNotices();

        // 7. REPORTS & ANALYTICS
        public void ShowReportsAnalytics()
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff", "Reports & Analytics")) return;
            ActiveSection = "ReportsAnalytics";
            CurrentViewModel = new ReportsAnalyticsViewModel(
                _heatmapService,
                _recidivismService,
                _pdfService,
                _commService,
                _incidentService);
        }

        public void ShowCampusHeatmap() => ShowReportsAnalytics();
        public void ShowRecidivism() => ShowReportsAnalytics();
        public void ShowExecutiveReports() => ShowReportsAnalytics();

        // 8. SETTINGS
        public void ShowSettingsWorkspace(int tabIndex = 0)
        {
            if (!EnsureRolePermission("System Administrator,Discipline Officer", "System Settings")) return;
            ActiveSection = "Settings";
            var vm = new SettingsWorkspaceViewModel(
                _settingsService,
                _themeService,
                _classificationService,
                _auditService,
                _integrityService,
                _securityLogService,
                _commService,
                _pdfService);
            vm.SelectedTabIndex = tabIndex;
            CurrentViewModel = vm;
        }

        public void ShowSystemSettings() => ShowSettingsWorkspace(4);
        public void ShowUserManagement() => ShowSettingsWorkspace(0);
        public void ShowRiskRules() => ShowSettingsWorkspace(3);
        public void ShowAuditTrail() => ShowSettingsWorkspace(5);

        private bool EnsureRolePermission(string allowedRoles, string moduleName)
        {
            if (SessionManager.HasPermission(allowedRoles))
                return true;

            MessageBox.Show(
                $"Access Denied: You do not have authorization to access the '{moduleName}' module.\nRequired Roles: {allowedRoles}\nYour Current Role: {CurrentUserRole}",
                "Access Control Violation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        private void NotifyActiveStates()
        {
            OnPropertyChanged(nameof(IsDashboardActive));
            OnPropertyChanged(nameof(IsDashboardOverviewActive));
            OnPropertyChanged(nameof(IsStudentsActive));
            OnPropertyChanged(nameof(IsIncidentsActive));
            OnPropertyChanged(nameof(IsInvestigationsActive));
            OnPropertyChanged(nameof(IsBehaviorActive));
            OnPropertyChanged(nameof(IsParentNoticesActive));
            OnPropertyChanged(nameof(IsReportsAnalyticsActive));
            OnPropertyChanged(nameof(IsSettingsActive));
            OnPropertyChanged(nameof(ActiveSectionTitle));
        }
    }
}
