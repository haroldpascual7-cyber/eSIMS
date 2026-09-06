using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class SettingsWorkspaceViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly IIncidentClassificationService _classificationService;
        private readonly IAuditService _auditService;
        private readonly IIntegrityAuditService _integrityService;
        private readonly ISecurityAccessLogService _securityLogService;
        private readonly ICommunicationService _commService;
        private readonly IPdfExportService _pdfService;

        private int _selectedTabIndex;
        private bool _isLoading;

        // --- Embedded Child ViewModels ---
        public UserManagementViewModel UserManagementVM { get; }
        public RbacViewModel RbacVM { get; }
        public KeywordMatrixViewModel KeywordVM { get; }
        public LocationWeightingViewModel LocationVM { get; }
        public RepeatOffenderViewModel RepeatOffenderVM { get; }
        public ActivityLogViewModel ActivityLogVM { get; }
        public IntegrityAuditViewModel IntegrityVM { get; }
        public SecurityAccessLogViewModel SecurityLogVM { get; }

        // --- Email & SMTP Configuration State ---
        private string _smtpHost = "smtp.gmail.com";
        private string _smtpPort = "587";
        private string _smtpSenderEmail = "alerts@smartacademy.edu.ph";
        private string _smtpSenderName = "Smart Academy Student Affairs";
        private string _smtpPassword = "••••••••••••";
        private bool _smtpEnableSsl = true;
        private bool _notificationsEnabled = true;
        private string _testRecipientEmail = string.Empty;
        private string _emailStatusMessage = string.Empty;

        // --- Branding & System Maintenance State ---
        private string _schoolName = "Matti National High School";
        private string _systemTitle = "Smart School Incident Management System";
        private string _motto = "Safe Campus • Real-time Reporting • Smart Resolution";
        private string _tagline = "Manage. Monitor. Protect.";
        private string _brandingStatusMessage = string.Empty;
        private string _backupStatusMessage = string.Empty;
        private string _lastBackupDate = "No backups created in this session";

        public SettingsWorkspaceViewModel(
            ISettingsService settingsService,
            IThemeService themeService,
            IIncidentClassificationService classificationService,
            IAuditService auditService,
            IIntegrityAuditService integrityService,
            ISecurityAccessLogService securityLogService,
            ICommunicationService commService,
            IPdfExportService pdfService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
            _securityLogService = securityLogService ?? throw new ArgumentNullException(nameof(securityLogService));
            _commService = commService ?? throw new ArgumentNullException(nameof(commService));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));

            // Initialize Child ViewModels with dummy callbacks (since they operate directly within workspace tabs)
            UserManagementVM = new UserManagementViewModel(() => { });
            RbacVM = new RbacViewModel(() => { });
            KeywordVM = new KeywordMatrixViewModel(_classificationService, () => { });
            LocationVM = new LocationWeightingViewModel(_classificationService, () => { });
            RepeatOffenderVM = new RepeatOffenderViewModel(_classificationService, () => { });
            ActivityLogVM = new ActivityLogViewModel(_auditService, _pdfService, () => { });
            IntegrityVM = new IntegrityAuditViewModel(_integrityService, _pdfService, () => { });
            SecurityLogVM = new SecurityAccessLogViewModel(_securityLogService, _pdfService, () => { });

            // Commands
            SaveEmailSettingsCommand = new RelayCommand(async _ => await SaveEmailSettingsAsync(), _ => CanManageSettings);
            SendTestEmailCommand = new RelayCommand(async _ => await SendTestEmailAsync(), _ => CanManageSettings);
            SaveBrandingCommand = new RelayCommand(async _ => await SaveBrandingAsync(), _ => CanManageSettings);
            CreateBackupCommand = new RelayCommand(async _ => await CreateDatabaseBackupAsync(), _ => CanManageSettings);
            ToggleThemeCommand = new RelayCommand(_ =>
            {
                _themeService.ToggleTheme();
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(ThemeModeText));
            });

            _ = InitializeSettingsAsync();
        }

        public bool IsAdmin => SessionManager.HasPermission("System Administrator");
        public bool CanManageSettings => SessionManager.HasPermission("System Administrator,Discipline Officer");

        public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        // --- Email Properties ---
        public string SmtpHost { get => _smtpHost; set => SetProperty(ref _smtpHost, value); }
        public string SmtpPort { get => _smtpPort; set => SetProperty(ref _smtpPort, value); }
        public string SmtpSenderEmail { get => _smtpSenderEmail; set => SetProperty(ref _smtpSenderEmail, value); }
        public string SmtpSenderName { get => _smtpSenderName; set => SetProperty(ref _smtpSenderName, value); }
        public string SmtpPassword { get => _smtpPassword; set => SetProperty(ref _smtpPassword, value); }
        public bool SmtpEnableSsl { get => _smtpEnableSsl; set => SetProperty(ref _smtpEnableSsl, value); }
        public bool NotificationsEnabled { get => _notificationsEnabled; set => SetProperty(ref _notificationsEnabled, value); }
        public string TestRecipientEmail { get => _testRecipientEmail; set => SetProperty(ref _testRecipientEmail, value); }
        public string EmailStatusMessage { get => _emailStatusMessage; set => SetProperty(ref _emailStatusMessage, value); }

        // --- Branding Properties ---
        public string SchoolName { get => _schoolName; set => SetProperty(ref _schoolName, value); }
        public string SystemTitle { get => _systemTitle; set => SetProperty(ref _systemTitle, value); }
        public string Motto { get => _motto; set => SetProperty(ref _motto, value); }
        public string Tagline { get => _tagline; set => SetProperty(ref _tagline, value); }
        public string BrandingStatusMessage { get => _brandingStatusMessage; set => SetProperty(ref _brandingStatusMessage, value); }
        public string BackupStatusMessage { get => _backupStatusMessage; set => SetProperty(ref _backupStatusMessage, value); }
        public string LastBackupDate { get => _lastBackupDate; set => SetProperty(ref _lastBackupDate, value); }

        public bool IsDarkMode => _themeService.CurrentTheme == AppTheme.Dark;
        public string ThemeModeText => IsDarkMode ? "Dark Mode" : "Light Mode";

        // Commands
        public ICommand SaveEmailSettingsCommand { get; }
        public ICommand SendTestEmailCommand { get; }
        public ICommand SaveBrandingCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        private async Task InitializeSettingsAsync()
        {
            IsLoading = true;
            try
            {
                await _settingsService.LoadSettingsAsync();
                SchoolName = _settingsService.SchoolName;
                SystemTitle = _settingsService.SystemTitle;
                Motto = _settingsService.Motto;
                Tagline = _settingsService.Tagline;

                SmtpHost = await _settingsService.GetSettingAsync("SmtpHost", "smtp.gmail.com");
                SmtpPort = await _settingsService.GetSettingAsync("SmtpPort", "587");
                SmtpSenderEmail = await _settingsService.GetSettingAsync("SmtpSenderEmail", "alerts@smartacademy.edu.ph");
                SmtpSenderName = await _settingsService.GetSettingAsync("SmtpSenderName", "Smart Academy Student Affairs");
                SmtpEnableSsl = bool.Parse(await _settingsService.GetSettingAsync("SmtpEnableSsl", "True"));
                NotificationsEnabled = bool.Parse(await _settingsService.GetSettingAsync("NotificationsEnabled", "True"));
                TestRecipientEmail = SmtpSenderEmail;
            }
            catch { }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveEmailSettingsAsync()
        {
            try
            {
                await _settingsService.SetSettingAsync("SmtpHost", SmtpHost, "SMTP Server Host");
                await _settingsService.SetSettingAsync("SmtpPort", SmtpPort, "SMTP Server Port");
                await _settingsService.SetSettingAsync("SmtpSenderEmail", SmtpSenderEmail, "Sender Email");
                await _settingsService.SetSettingAsync("SmtpSenderName", SmtpSenderName, "Sender Name");
                await _settingsService.SetSettingAsync("SmtpEnableSsl", SmtpEnableSsl.ToString(), "Use SSL/TLS");
                await _settingsService.SetSettingAsync("NotificationsEnabled", NotificationsEnabled.ToString(), "Enable Automated Notifications");

                EmailStatusMessage = "Email settings saved successfully.";
                MessageBox.Show("SMTP Configuration successfully updated.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                EmailStatusMessage = $"Failed to save email settings: {ex.Message}";
                MessageBox.Show(EmailStatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendTestEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(TestRecipientEmail))
            {
                MessageBox.Show("Please specify a test recipient email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EmailStatusMessage = "Sending test message...";
                bool sent = await _commService.SendEmailAsync(
                    "TEST-001",
                    "Administrator",
                    TestRecipientEmail,
                    "Smart School Incident System: SMTP Test Verification",
                    $"This is an automated test communication confirming that the SMTP notification service is functioning correctly on {DateTime.Now:yyyy-MM-dd HH:mm:ss}.");

                if (sent)
                {
                    EmailStatusMessage = "Test message delivered successfully!";
                    MessageBox.Show($"Verification email successfully sent to:\n{TestRecipientEmail}", "Test Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                EmailStatusMessage = $"Dispatch failed: {ex.Message}";
                MessageBox.Show(EmailStatusMessage, "Dispatch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveBrandingAsync()
        {
            try
            {
                await _settingsService.SetSettingAsync("SchoolName", SchoolName, "School Name");
                await _settingsService.SetSettingAsync("SystemTitle", SystemTitle, "System Application Title");
                await _settingsService.SetSettingAsync("Motto", Motto, "School Motto");
                await _settingsService.SetSettingAsync("Tagline", Tagline, "System Tagline");

                BrandingStatusMessage = "School branding updated successfully.";
                MessageBox.Show("Branding changes saved successfully.", "Branding Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                BrandingStatusMessage = $"Error: {ex.Message}";
                MessageBox.Show(BrandingStatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateDatabaseBackupAsync()
        {
            try
            {
                BackupStatusMessage = "Creating backup snapshot...";
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string backupDir = Path.Combine(appData, "SmartSchoolIncidentManagement", "Backups");
                Directory.CreateDirectory(backupDir);

                string dbFile = Path.Combine(appData, "SmartSchoolIncidentManagement", "school_incidents.db");
                string backupFile = Path.Combine(backupDir, $"school_incidents_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                await Task.Run(() =>
                {
                    if (File.Exists(dbFile))
                    {
                        File.Copy(dbFile, backupFile, overwrite: true);
                    }
                });

                LastBackupDate = DateTime.Now.ToString("MMM dd, yyyy HH:mm:ss");
                BackupStatusMessage = $"Backup successfully created:\n{backupFile}";
                MessageBox.Show($"Database snapshot created at:\n{backupFile}", "Backup Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                BackupStatusMessage = $"Backup failed: {ex.Message}";
                MessageBox.Show(BackupStatusMessage, "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
