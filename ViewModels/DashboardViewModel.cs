using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
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
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly ISettingsService _settingsService;
        private readonly Action<Incident?> _openDetailAction;
        private readonly Action _openIncidentListAction;

        private int _totalIncidents;
        private int _openIncidents;
        private int _inProgressIncidents;
        private int _resolvedIncidents;
        private int _criticalIncidents;
        private int _totalStudents;
        private int _totalStaff;
        private int _totalInvestigations;
        private int _totalActionPlans;
        private int _totalParentNotices;

        public DashboardViewModel(
            IIncidentService incidentService,
            Action<Incident?> openDetailAction,
            Action openIncidentListAction,
            Action? returnToTabletDashboardAction = null,
            ISettingsService? settingsService = null)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _settingsService = settingsService ?? new SettingsService(new AppDbContext());
            _openDetailAction = openDetailAction ?? throw new ArgumentNullException(nameof(openDetailAction));
            _openIncidentListAction = openIncidentListAction ?? throw new ArgumentNullException(nameof(openIncidentListAction));

            _settingsService.SettingsChanged += RefreshBranding;
            _ = _settingsService.LoadSettingsAsync();

            RecentIncidents = new ObservableCollection<Incident>();

            // --- Navigation Commands ---
            NavigateDashboardCommand = new RelayCommand(async () => await LoadDataAsync());
            NavigateReportNewIncidentCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowIncidents(createNew: true)));
            NavigateIncidentListCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowIncidents()));
            NavigateMyFiledReportsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowIncidents()));
            NavigateSchoolNoticeCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowParentNotices()));
            NavigateInvestigationsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowInvestigations()));
            NavigateDisciplinaryActionsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowInvestigations()));
            NavigateRiskRulesCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowRiskRules()));
            NavigateStudentProfileCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowStudents()));
            NavigateStudentsCommand = NavigateStudentProfileCommand;
            NavigateBehaviorCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowBehaviorWorkspace()));
            NavigateParentNoticesCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowParentNotices()));
            NavigateReportsAnalyticsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowReportsAnalytics()));
            NavigateAuditTrailCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowAuditTrail()));
            NavigateUserManagementCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowUserManagement()));
            NavigateSystemSettingsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowSystemSettings()));
            ReturnToTabletDashboardCommand = new RelayCommand(() =>
            {
                if (returnToTabletDashboardAction != null) returnToTabletDashboardAction();
                else NavigateTo(vm => vm.ShowDashboard());
            });

            NewIncidentCommand = NavigateReportNewIncidentCommand;
            ViewAllIncidentsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowIncidents()));
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshAsync(), () => !IsRefreshing);
            ViewNotificationsCommand = new RelayCommand(ViewNotifications);
            ToggleThemeCommand = new RelayCommand(() => NavigateTo(vm => vm.ToggleThemeCommand.Execute(null)));
            LogoutCommand = new RelayCommand(() => NavigateTo(vm => vm.LogoutCommand.Execute(null)));

            SelectIncidentCommand = new RelayCommand(param =>
            {
                if (param is Incident inc)
                {
                    NavigateTo(vm => vm.ShowStudentInvestigationInquiry(inc));
                }
            });

            InitializeCaseloadSupport();
            _ = LoadDataAsync();
        }

        private void NavigateTo(Action<MainViewModel> action)
        {
            var app = System.Windows.Application.Current;
            if (app?.MainWindow?.DataContext is MainViewModel mainVm)
            {
                action(mainVm);
            }
        }

        // ---------- Dynamic Time-Based Greeting Properties ----------
        public string TimeGreeting
        {
            get
            {
                int hour = DateTime.Now.Hour;
                if (hour < 12)
                    return "Good Morning ☀️";
                if (hour < 18)
                    return "Good Afternoon 🌤️";
                return "Good Evening 🌙";
            }
        }

        public string UserFullName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SessionManager.FullName))
                    return SessionManager.FullName;
                if (!string.IsNullOrWhiteSpace(SessionManager.Username))
                    return SessionManager.Username;
                return "Administrator";
            }
        }

        public string UserRole => string.IsNullOrWhiteSpace(SessionManager.Role) ? "System Administrator" : SessionManager.Role;

        public string DashboardTitle
        {
            get
            {
                var role = UserRole;
                if (string.IsNullOrWhiteSpace(role) || role.Equals("Administrator", StringComparison.OrdinalIgnoreCase) || role.Equals("System Administrator", StringComparison.OrdinalIgnoreCase))
                    return "Administrator Dashboard";
                if (role.Equals("Guidance Counselor", StringComparison.OrdinalIgnoreCase))
                    return "Guidance Counselor Dashboard";
                if (role.Equals("Discipline Officer", StringComparison.OrdinalIgnoreCase))
                    return "Discipline Officer Dashboard";
                if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase) || role.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                    return "Staff Dashboard";
                return $"{role} Dashboard";
            }
        }

        public string FullGreetingHeader => $"{TimeGreeting}, {UserFullName}!";

        public string SchoolName => _settingsService.SchoolName;
        public string SystemTitle => _settingsService.SystemTitle;
        public object? DashboardLogoSource => _settingsService.DashboardLogoSource;
        public string LogoIcon => _settingsService.LogoIcon;
        public bool HasDashboardLogo => DashboardLogoSource != null;

        private void RefreshBranding()
        {
            OnPropertyChanged(nameof(SchoolName));
            OnPropertyChanged(nameof(SystemTitle));
            OnPropertyChanged(nameof(DashboardLogoSource));
            OnPropertyChanged(nameof(LogoIcon));
            OnPropertyChanged(nameof(HasDashboardLogo));
            OnPropertyChanged(nameof(RoleSubheader));
        }

        public string RoleSubheader => $"You are logged in as {UserRole} • {SchoolName}";

        private string _totalIncidentsSubtitle = "All safety reports";
        public string TotalIncidentsSubtitle
        {
            get => _totalIncidentsSubtitle;
            set => SetProperty(ref _totalIncidentsSubtitle, value);
        }

        public int TotalIncidents
        {
            get => _totalIncidents;
            set => SetProperty(ref _totalIncidents, value);
        }

        public int OpenIncidents
        {
            get => _openIncidents;
            set => SetProperty(ref _openIncidents, value);
        }

        public int InProgressIncidents
        {
            get => _inProgressIncidents;
            set => SetProperty(ref _inProgressIncidents, value);
        }

        public int ResolvedIncidents
        {
            get => _resolvedIncidents;
            set => SetProperty(ref _resolvedIncidents, value);
        }

        public int CriticalIncidents
        {
            get => _criticalIncidents;
            set => SetProperty(ref _criticalIncidents, value);
        }

        public int TotalStudents
        {
            get => _totalStudents;
            set => SetProperty(ref _totalStudents, value);
        }

        public int TotalStaff
        {
            get => _totalStaff;
            set => SetProperty(ref _totalStaff, value);
        }

        public int TotalInvestigations
        {
            get => _totalInvestigations;
            set => SetProperty(ref _totalInvestigations, value);
        }

        public int TotalActionPlans
        {
            get => _totalActionPlans;
            set => SetProperty(ref _totalActionPlans, value);
        }

        public int TotalParentNotices
        {
            get => _totalParentNotices;
            set => SetProperty(ref _totalParentNotices, value);
        }

        public ICommand ReturnToTabletDashboardCommand { get; }
        public ICommand NavigateStudentsCommand { get; }
        public ICommand NavigateBehaviorCommand { get; }

        private int _newIncidentCount;

        public int NewIncidentCount
        {
            get => _newIncidentCount;
            set
            {
                if (SetProperty(ref _newIncidentCount, value))
                {
                    OnPropertyChanged(nameof(NotificationBadgeText));
                    OnPropertyChanged(nameof(HasNewNotifications));
                }
            }
        }

        public bool HasNewNotifications => NewIncidentCount > 0;

        public string NotificationBadgeText => NewIncidentCount == 1 ? "1 New Report" : $"{NewIncidentCount} New Reports";

        public bool IsNotificationBadgeVisible => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");

        public bool IsNewIncidentButtonVisible => SessionManager.Role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) || SessionManager.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase);

        public ObservableCollection<Incident> RecentIncidents { get; }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateReportNewIncidentCommand { get; }
        public ICommand NavigateIncidentListCommand { get; }
        public ICommand NavigateMyFiledReportsCommand { get; }
        public ICommand NavigateSchoolNoticeCommand { get; }
        public ICommand NavigateInvestigationsCommand { get; }
        public ICommand NavigateDisciplinaryActionsCommand { get; }
        public ICommand NavigateRiskRulesCommand { get; }
        public ICommand NavigateStudentProfileCommand { get; }
        public ICommand NavigateParentNoticesCommand { get; }
        public ICommand NavigateReportsAnalyticsCommand { get; }
        public ICommand NavigateAuditTrailCommand { get; }
        public ICommand NavigateUserManagementCommand { get; }
        public ICommand NavigateSystemSettingsCommand { get; }

        public ICommand NewIncidentCommand { get; }
        public ICommand ViewAllIncidentsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewNotificationsCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SelectIncidentCommand { get; }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(IsLoading));
                    OnPropertyChanged(nameof(RefreshButtonText));
                }
            }
        }

        public bool IsLoading => IsRefreshing;
        public bool IsNotRefreshing => !IsRefreshing;
        public string RefreshButtonText => IsRefreshing ? "⌛ Refreshing..." : "↻ Refresh";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing) return;
            try
            {
                IsRefreshing = true;
                await LoadDataAsync();
                await Task.Delay(750);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public async Task LoadDataAsync()
        {
            var all = (await _incidentService.GetAllIncidentsAsync()).ToList();
            ProcessScopeData(all);

            try
            {
                using var context = new AppDbContext();
                TotalStudents = await context.Students.CountAsync();
                TotalStaff = await context.StaffMembers.CountAsync();

                // Active inquiries: count investigations currently open (ClosedDate == null) or InProgressIncidents
                var activeInvCount = await context.Investigations.CountAsync(inv => inv.ClosedDate == null);
                TotalInvestigations = activeInvCount > 0 ? activeInvCount : InProgressIncidents;

                int plans = await context.RestorativePlans.CountAsync();
                int merits = await context.StudentMeritLogs.CountAsync();
                TotalActionPlans = plans + merits;

                try
                {
                    var commService = new CommunicationService(context);
                    var logs = await commService.GetRecentDispatchesAsync();
                    TotalParentNotices = logs?.Count ?? 6;
                }
                catch
                {
                    TotalParentNotices = 6;
                }
            }
            catch
            {
                TotalStudents = 7;
                TotalStaff = 4;
                TotalInvestigations = InProgressIncidents > 0 ? InProgressIncidents : 3;
                TotalActionPlans = 6;
                TotalParentNotices = 6;
            }

            OnPropertyChanged(nameof(TimeGreeting));
            OnPropertyChanged(nameof(UserFullName));
            OnPropertyChanged(nameof(UserRole));
            OnPropertyChanged(nameof(FullGreetingHeader));
            OnPropertyChanged(nameof(RoleSubheader));
            OnPropertyChanged(nameof(IsNotificationBadgeVisible));
            OnPropertyChanged(nameof(IsNewIncidentButtonVisible));
        }

        private void ViewNotifications()
        {
            NavigateTo(vm => vm.OpenNotificationModal());
        }
    }
}