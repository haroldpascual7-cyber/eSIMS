using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;

namespace SmartSchoolIncidentManagementSystem
{
    public partial class App : Application
    {
        private AppDbContext _context = null!;
        private IIncidentService _incidentService = null!;
        private IThemeService _themeService = null!;
        private IAuthService _authService = null!;
        private ISettingsService _settingsService = null!;
        private IRuleEngineService _ruleEngineService = null!;
        private IInvestigationService _investigationService = null!;
        private IConsequenceService _consequenceService = null!;
        private IRestorativeService _restorativeService = null!;
        private ISuspensionService _suspensionService = null!;
        private IProbationService _probationService = null!;
        private IIncidentClassificationService _classificationService = null!;
        private IBehavioralTranscriptService _transcriptService = null!;
        private IAnalyticsService _analyticsService = null!;
        private IActionPlanService _planService = null!;
        private IPdfExportService _pdfService = null!;
        private ILetterTemplateService _templateService = null!;
        private ICommunicationService _commService = null!;
        private IAcknowledgmentService _ackService = null!;
        private IHeatmapService _heatmapService = null!;
        private IRecidivismService _recidivismService = null!;
        private IAuditService _auditService = null!;
        private IIntegrityAuditService _integrityService = null!;
        private ISecurityAccessLogService _securityLogService = null!;
        private IStudentService _studentService = null!;
        private IAnecdotalService _anecdotalService = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            try
            {
                // Initialize SQLite DB and seed initial data
                _context = new AppDbContext();
                DbInitializer.Initialize(_context);

                // Initialize SystemUsers, RolePermissions, and StaffRoster tables
                DatabaseInitializer.InitializeAllTables();

                // Initialize Services
                _incidentService = new IncidentService(_context);
                _themeService = new ThemeService();
                _authService = new AuthService(_context);
                _settingsService = new SettingsService(_context);
                _ruleEngineService = new RuleEngineService(_context);
                _investigationService = new InvestigationService(_context);
                _consequenceService = new ConsequenceService(_context);
                _restorativeService = new RestorativeService(_context);
                _suspensionService = new SuspensionService(_context);
                _probationService = new ProbationService(_context);
                _classificationService = new IncidentClassificationService(_context);
                _transcriptService = new BehavioralTranscriptService(_context);
                _analyticsService = new AnalyticsService(_context);
                _planService = new ActionPlanService(_context);
                _pdfService = new PdfExportService();
                _templateService = new LetterTemplateService(_context);
                _commService = new CommunicationService(_context);
                _ackService = new AcknowledgmentService(_context);
                _heatmapService = new HeatmapService(_context);
                _recidivismService = new RecidivismService(_context);
                _auditService = new AuditService(_context);
                _integrityService = new IntegrityAuditService(_context);
                _securityLogService = new SecurityAccessLogService(_context);
                _studentService = new StudentService(_context);
                _anecdotalService = new AnecdotalService();

                // Main View Model
                var mainViewModel = new MainViewModel(
                    _incidentService,
                    _themeService,
                    _authService,
                    _settingsService,
                    _ruleEngineService,
                    _investigationService,
                    _consequenceService,
                    _restorativeService,
                    _suspensionService,
                    _probationService,
                    _classificationService,
                    _transcriptService,
                    _analyticsService,
                    _planService,
                    _templateService,
                    _pdfService,
                    _commService,
                    _ackService,
                    _heatmapService,
                    _recidivismService,
                    _auditService,
                    _integrityService,
                    _securityLogService,
                    _studentService,
                    _anecdotalService);

                // Window Setup
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup error:\n\n{ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unexpected error occurred:\n\n{e.Exception.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Critical error:\n\n{ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _context?.Dispose();
            base.OnExit(e);
        }
    }
}
