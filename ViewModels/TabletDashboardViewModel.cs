using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class TabletDashboardViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly Action<Incident?> _openDetailAction;
        private readonly Action _openIncidentListAction;
        private readonly System.Windows.Threading.DispatcherTimer _clockTimer;
        private int _newIncidentCount;
        private string _currentTime = string.Empty;
        private string _currentDate = string.Empty;
        private string _timeGreeting = string.Empty;

        public ObservableCollection<DashboardCardItem> VisibleCards { get; } = new();

        public TabletDashboardViewModel(IIncidentService incidentService, Action<Incident?> openDetailAction, Action openIncidentListAction)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _openDetailAction = openDetailAction ?? throw new ArgumentNullException(nameof(openDetailAction));
            _openIncidentListAction = openIncidentListAction ?? throw new ArgumentNullException(nameof(openIncidentListAction));

            // --- Real-time Live Clock & Date Dispatcher Timer ---
            UpdateTime();
            _clockTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateTime();
            _clockTimer.Start();

            // --- 14 Metro Tile Dashboard Navigation Commands ---
            NavigateDashboardCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowOperationalDashboard()));
            NavigateReportNewIncidentCommand = new RelayCommand(() => { if (CanAccessIntake) NavigateTo(vm => vm.ShowIncidents()); });
            NavigateIncidentListCommand = new RelayCommand(() => { if (CanAccessIntake) NavigateTo(vm => vm.ShowIncidents()); });
            NavigateMyFiledReportsCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowIncidents()));
            NavigateSchoolNoticeCommand = new RelayCommand(() => NavigateTo(vm => vm.ShowParentNotices()));
            NavigateInvestigationsCommand = new RelayCommand(() => { if (CanAccessInvestigations) NavigateTo(vm => vm.ShowInvestigationList()); });
            NavigateDisciplinaryActionsCommand = new RelayCommand(() => { if (CanAccessDisciplinary) NavigateTo(vm => vm.ShowDisciplinaryActions()); });
            NavigateRiskRulesCommand = new RelayCommand(() => { if (CanAccessRiskRules) NavigateTo(vm => vm.ShowRiskRules()); });
            NavigateStudentProfileCommand = new RelayCommand(() => { if (CanAccessStudentProfiles) NavigateTo(vm => vm.ShowStudentProfiles()); });
            NavigateParentNoticesCommand = new RelayCommand(() => { if (CanAccessParentNotices) NavigateTo(vm => vm.ShowParentNotices()); });
            NavigateReportsAnalyticsCommand = new RelayCommand(() => { if (CanAccessReportsAnalytics) NavigateTo(vm => vm.ShowReportsAnalytics()); });
            NavigateAuditTrailCommand = new RelayCommand(() => { if (CanAccessAuditTrail) NavigateTo(vm => vm.ShowAuditTrail()); });
            NavigateUserManagementCommand = new RelayCommand(() => { if (CanAccessUserManagement) NavigateTo(vm => vm.ShowUserManagement()); });
            NavigateSystemSettingsCommand = new RelayCommand(() => { if (CanAccessSystemSettings) NavigateTo(vm => vm.ShowSystemSettings()); });

            ViewNotificationsCommand = new RelayCommand(ViewNotifications);
            ToggleThemeCommand = new RelayCommand(() => NavigateTo(vm => vm.ToggleThemeCommand.Execute(null)));
            LogoutCommand = new RelayCommand(() => NavigateTo(vm => vm.LogoutCommand.Execute(null)));
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshAsync(), () => !IsRefreshing);

            SessionManager.PermissionsChanged += RefreshPermissions;
            RebuildVisibleCards();
            _ = LoadNotificationsAsync();
        }

        public void RefreshPermissions()
        {
            OnPropertyChanged(nameof(CanAccessIntake));
            OnPropertyChanged(nameof(CanAccessInvestigations));
            OnPropertyChanged(nameof(CanAccessDisciplinary));
            OnPropertyChanged(nameof(CanAccessRiskRules));
            OnPropertyChanged(nameof(CanAccessStudentProfiles));
            OnPropertyChanged(nameof(CanAccessParentNotices));
            OnPropertyChanged(nameof(CanAccessReportsAnalytics));
            OnPropertyChanged(nameof(CanAccessAuditTrail));
            OnPropertyChanged(nameof(CanAccessUserManagement));
            OnPropertyChanged(nameof(CanAccessSystemSettings));
            OnPropertyChanged(nameof(IsNotificationBadgeVisible));
            OnPropertyChanged(nameof(IsStaffOrTeacher));

            RebuildVisibleCards();
        }

        private static Brush MakeBrush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

        public void RebuildVisibleCards()
        {
            var app = System.Windows.Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(RebuildVisibleCards);
                return;
            }

            VisibleCards.Clear();

            // 1. Dashboard
            VisibleCards.Add(new DashboardCardItem
            {
                Id = "Dashboard",
                Title = "Dashboard",
                Subtitle = "Alerts, KPIs, & actions.",
                Icon = "📊",
                BackgroundBrush = MakeBrush("#0078D7"),
                Command = NavigateDashboardCommand
            });

            // 2. Incident Intake & Reporting
            if (CanAccessIntake)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "ReportIncident",
                    Title = "Report Incident",
                    Subtitle = "Quick intake shortcut.",
                    Icon = "📝",
                    BackgroundBrush = MakeBrush("#107C41"),
                    Command = NavigateReportNewIncidentCommand
                });
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "IncidentList",
                    Title = "Incident List",
                    Subtitle = "Browse & filter records.",
                    Icon = "📋",
                    BackgroundBrush = MakeBrush("#0D9488"),
                    Command = NavigateIncidentListCommand
                });
            }

            // 3. My Filed Reports
            VisibleCards.Add(new DashboardCardItem
            {
                Id = "MyFiledReports",
                Title = "My Filed Reports",
                Subtitle = "Personal incident filings.",
                Icon = "📂",
                BackgroundBrush = MakeBrush("#0063B1"),
                Command = NavigateMyFiledReportsCommand
            });

            // 4. Investigations & Evidence
            if (CanAccessInvestigations)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "Investigations",
                    Title = "Investigations",
                    Subtitle = "Evidence & case logs.",
                    Icon = "🔍",
                    BackgroundBrush = MakeBrush("#1E293B"),
                    Command = NavigateInvestigationsCommand
                });
            }

            // 5. Disciplinary Actions & Sanctions
            if (CanAccessDisciplinary)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "Disciplinary",
                    Title = "Disciplinary",
                    Subtitle = "Sanctions & plans.",
                    Icon = "⚖️",
                    BackgroundBrush = MakeBrush("#334155"),
                    Command = NavigateDisciplinaryActionsCommand
                });
            }

            // 6. Risk Classification Rules
            if (CanAccessRiskRules)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "RiskRules",
                    Title = "Risk & Rules",
                    Subtitle = "Matrix & evaluation.",
                    Icon = "⚡",
                    BackgroundBrush = MakeBrush("#D97706"),
                    Command = NavigateRiskRulesCommand
                });
            }

            // 7. Student Profiles & IAPs
            if (CanAccessStudentProfiles)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "StudentProfile",
                    Title = "Student Profile",
                    Subtitle = "Behavioral histories.",
                    Icon = "👨‍🎓",
                    BackgroundBrush = MakeBrush("#0284C7"),
                    Command = NavigateStudentProfileCommand
                });
            }

            // 8. Parent Notices & Letters
            if (CanAccessParentNotices)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "ParentNotices",
                    Title = "Parent Notices",
                    Subtitle = "Letters & tracking.",
                    Icon = "✉️",
                    BackgroundBrush = MakeBrush("#0891B2"),
                    Command = NavigateParentNoticesCommand
                });
            }

            // 9. Reports & Safety Analytics
            if (CanAccessReportsAnalytics)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "ReportsAnalytics",
                    Title = "Reports & Data",
                    Subtitle = "Heatmaps & trends.",
                    Icon = "📈",
                    BackgroundBrush = MakeBrush("#7C3AED"),
                    Command = NavigateReportsAnalyticsCommand
                });
            }

            // 10. Audit Trail & Security Logs
            if (CanAccessAuditTrail)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "AuditTrail",
                    Title = "Audit Trail",
                    Subtitle = "Tamper-evident logs.",
                    Icon = "🔒",
                    BackgroundBrush = MakeBrush("#0F766E"),
                    Command = NavigateAuditTrailCommand
                });
            }

            // 11. User Management
            if (CanAccessUserManagement)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "UserManagement",
                    Title = "User Management",
                    Subtitle = "Accounts & roles.",
                    Icon = "👥",
                    BackgroundBrush = MakeBrush("#4338CA"),
                    Command = NavigateUserManagementCommand
                });
            }

            // 12. System Settings
            if (CanAccessSystemSettings)
            {
                VisibleCards.Add(new DashboardCardItem
                {
                    Id = "SystemSettings",
                    Title = "System Settings",
                    Subtitle = "Preferences & options.",
                    Icon = "⚙️",
                    BackgroundBrush = MakeBrush("#334155"),
                    Command = NavigateSystemSettingsCommand
                });
            }

            // 13. Exit / Log Out
            VisibleCards.Add(new DashboardCardItem
            {
                Id = "Logout",
                Title = "Exit / Log Out",
                Subtitle = "Close application.",
                Icon = "🚪",
                BackgroundBrush = MakeBrush("#B91C1C"),
                Command = LogoutCommand
            });
        }

        public bool CanAccessIntake => SessionManager.HasModuleAccess("Incident Intake & Reporting", "System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessInvestigations => SessionManager.HasModuleAccess("Investigations & Evidence", "System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessDisciplinary => SessionManager.HasModuleAccess("Disciplinary Actions & Sanctions", "System Administrator,Discipline Officer,Guidance Counselor");
        public bool CanAccessRiskRules => SessionManager.HasModuleAccess("Disciplinary Actions & Sanctions", "System Administrator,Discipline Officer,Guidance Counselor");
        public bool CanAccessStudentProfiles => SessionManager.HasModuleAccess("Student Profiles & IAPs", "System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessParentNotices => SessionManager.HasModuleAccess("Parent Notices & Letters", "System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessReportsAnalytics => SessionManager.HasModuleAccess("Reports & Safety Analytics", "System Administrator,Discipline Officer,Guidance Counselor,Teacher,Staff");
        public bool CanAccessAuditTrail => SessionManager.HasModuleAccess("Audit Trail & Security Logs", "System Administrator");
        public bool CanAccessUserManagement => SessionManager.HasModuleAccess("User Management & Settings", "System Administrator");
        public bool CanAccessSystemSettings => SessionManager.HasModuleAccess("System Settings", "System Administrator,Discipline Officer,Guidance Counselor");
        public bool IsStaffOrTeacher => !string.IsNullOrEmpty(UserRole) &&
            (UserRole.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0 ||
             UserRole.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0);

        private void UpdateTime()
        {
            var now = DateTime.Now;
            CurrentTime = now.ToString("h:mm:ss tt");
            CurrentDate = now.ToString("dddd, d MMMM yyyy");

            int hour = now.Hour;
            if (hour < 12)
                TimeGreeting = "Good morning";
            else if (hour < 18)
                TimeGreeting = "Good afternoon";
            else
                TimeGreeting = "Good evening";
        }

        private void NavigateTo(Action<MainViewModel> action)
        {
            var app = System.Windows.Application.Current;
            if (app?.MainWindow?.DataContext is MainViewModel mainVm)
            {
                action(mainVm);
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            private set => SetProperty(ref _currentTime, value);
        }

        public string CurrentDate
        {
            get => _currentDate;
            private set => SetProperty(ref _currentDate, value);
        }

        public string TimeGreeting
        {
            get => _timeGreeting;
            private set => SetProperty(ref _timeGreeting, value);
        }

        public string UserFullName => SessionManager.FullName;
        public string UserRole => SessionManager.Role;

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

        public string RoleSubheader => $"Operational Workspace • Role: {SessionManager.Role}";

        public int NewIncidentCount
        {
            get => _newIncidentCount;
            set => SetProperty(ref _newIncidentCount, value);
        }

        public bool IsNotificationBadgeVisible => true;

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

        public ICommand ViewNotificationsCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task LoadNotificationsAsync()
        {
            try
            {
                await NotificationService.Instance.InitializeAsync();
                NewIncidentCount = NotificationService.Instance.GetUnreadCountForUser(UserRole, SessionManager.Username);
            }
            catch
            {
                NewIncidentCount = 0;
            }
        }

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
                    OnPropertyChanged(nameof(RefreshButtonIcon));
                }
            }
        }

        public bool IsLoading => IsRefreshing;
        public bool IsNotRefreshing => !IsRefreshing;
        public string RefreshButtonIcon => IsRefreshing ? "⌛" : "↻";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing) return;
            try
            {
                IsRefreshing = true;
                RefreshPermissions();
                await LoadNotificationsAsync();
                RebuildVisibleCards();
                await Task.Delay(750);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void ViewNotifications()
        {
            var app = System.Windows.Application.Current;
            if (app?.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.ViewNotificationsCommand.Execute(null);
            }
        }
    }
}
