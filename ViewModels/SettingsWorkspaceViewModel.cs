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
        public AcademicStructureViewModel AcademicVM { get; }

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
        private string _schoolAddress = "Digos City, Davao Del Sur";
        private string _systemTitle = "Smart School Incident Management System";
        private string _systemAcronym = "eSIMS";
        private string _motto = "Safe Campus • Real-time Reporting • Smart Resolution";
        private string _tagline = "Manage. Monitor. Protect.";
        private string _loginLogoPath = string.Empty;
        private string _dashboardLogoPath = string.Empty;
        private bool _useSameLogoForDashboard = true;
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
            AcademicVM = new AcademicStructureViewModel(_settingsService, _auditService);

            // Commands
            SaveEmailSettingsCommand = new RelayCommand(async _ => await SaveEmailSettingsAsync(), _ => CanManageSettings);
            SendTestEmailCommand = new RelayCommand(async _ => await SendTestEmailAsync(), _ => CanManageSettings);
            SaveBrandingCommand = new RelayCommand(async _ => await SaveBrandingAsync(), _ => CanManageSettings);
            BrowseLoginLogoCommand = new RelayCommand(_ => BrowseLoginLogo(), _ => CanManageSettings);
            ResetLoginLogoCommand = new RelayCommand(_ => ResetLoginLogo(), _ => CanManageSettings);
            BrowseDashboardLogoCommand = new RelayCommand(_ => BrowseDashboardLogo(), _ => CanManageSettings && CanEditDashboardLogoCustom);
            ResetDashboardLogoCommand = new RelayCommand(_ => ResetDashboardLogo(), _ => CanManageSettings && CanEditDashboardLogoCustom);
            CreateBackupCommand = new RelayCommand(async _ => await CreateDatabaseBackupAsync(), _ => CanManageSettings);
            ToggleThemeCommand = new RelayCommand(_ =>
            {
                _themeService.ToggleTheme();
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(ThemeModeText));
                _ = _auditService.LogActivityAsync(
                    SessionManager.CurrentUsername ?? "ADMIN-01",
                    "CONFIG",
                    "Interface Theme",
                    $"Switched interface theme mode to {ThemeModeText}.");
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
        public string SchoolAddress { get => _schoolAddress; set => SetProperty(ref _schoolAddress, value); }
        public string SystemTitle { get => _systemTitle; set => SetProperty(ref _systemTitle, value); }
        public string SystemAcronym { get => _systemAcronym; set => SetProperty(ref _systemAcronym, value); }
        public string Motto { get => _motto; set => SetProperty(ref _motto, value); }
        public string Tagline { get => _tagline; set => SetProperty(ref _tagline, value); }
        public string LoginLogoPath
        {
            get => _loginLogoPath;
            set
            {
                if (SetProperty(ref _loginLogoPath, value))
                {
                    OnPropertyChanged(nameof(LoginLogoPreview));
                    OnPropertyChanged(nameof(HasCustomLoginLogo));
                    if (UseSameLogoForDashboard)
                    {
                        OnPropertyChanged(nameof(DashboardLogoPreview));
                    }
                }
            }
        }
        public string DashboardLogoPath
        {
            get => _dashboardLogoPath;
            set
            {
                if (SetProperty(ref _dashboardLogoPath, value))
                {
                    OnPropertyChanged(nameof(DashboardLogoPreview));
                    OnPropertyChanged(nameof(HasCustomDashboardLogo));
                }
            }
        }
        public bool UseSameLogoForDashboard
        {
            get => _useSameLogoForDashboard;
            set
            {
                if (SetProperty(ref _useSameLogoForDashboard, value))
                {
                    OnPropertyChanged(nameof(DashboardLogoPreview));
                    OnPropertyChanged(nameof(CanEditDashboardLogoCustom));
                }
            }
        }
        public bool CanEditDashboardLogoCustom => !UseSameLogoForDashboard;
        public bool HasCustomLoginLogo => !string.IsNullOrWhiteSpace(LoginLogoPath) && File.Exists(LoginLogoPath);
        public bool HasCustomDashboardLogo => !string.IsNullOrWhiteSpace(DashboardLogoPath) && File.Exists(DashboardLogoPath);

        public object? LoginLogoPreview => LoadImagePreview(LoginLogoPath, SettingsService.DefaultSchoolSealUri);
        public object? DashboardLogoPreview => UseSameLogoForDashboard
            ? LoginLogoPreview
            : LoadImagePreview(DashboardLogoPath, SettingsService.DefaultSchoolSealUri);

        public string BrandingStatusMessage { get => _brandingStatusMessage; set => SetProperty(ref _brandingStatusMessage, value); }
        public string BackupStatusMessage { get => _backupStatusMessage; set => SetProperty(ref _backupStatusMessage, value); }
        public string LastBackupDate { get => _lastBackupDate; set => SetProperty(ref _lastBackupDate, value); }

        public bool IsDarkMode => _themeService.CurrentTheme == AppTheme.Dark;
        public string ThemeModeText => IsDarkMode ? "Dark Mode" : "Light Mode";

        // Commands
        public ICommand SaveEmailSettingsCommand { get; }
        public ICommand SendTestEmailCommand { get; }
        public ICommand SaveBrandingCommand { get; }
        public ICommand BrowseLoginLogoCommand { get; }
        public ICommand ResetLoginLogoCommand { get; }
        public ICommand BrowseDashboardLogoCommand { get; }
        public ICommand ResetDashboardLogoCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        private async Task InitializeSettingsAsync()
        {
            IsLoading = true;
            try
            {
                await _settingsService.LoadSettingsAsync();
                SchoolName = _settingsService.SchoolName;
                SchoolAddress = _settingsService.SchoolAddress;
                SystemTitle = _settingsService.SystemTitle;
                SystemAcronym = _settingsService.SystemAcronym;
                Motto = _settingsService.Motto;
                Tagline = _settingsService.Tagline;
                LoginLogoPath = _settingsService.LoginLogoPath;
                DashboardLogoPath = _settingsService.DashboardLogoPath;
                UseSameLogoForDashboard = string.IsNullOrWhiteSpace(DashboardLogoPath);

                SmtpHost = await _settingsService.GetSettingAsync("SmtpHost", "smtp.gmail.com");
                SmtpPort = await _settingsService.GetSettingAsync("SmtpPort", "587");
                SmtpSenderEmail = await _settingsService.GetSettingAsync("SmtpSenderEmail", "alerts@smartacademy.edu.ph");
                SmtpSenderName = await _settingsService.GetSettingAsync("SmtpSenderName", "Smart Academy Student Affairs");
                SmtpEnableSsl = bool.Parse(await _settingsService.GetSettingAsync("SmtpEnableSsl", "True"));
                NotificationsEnabled = bool.Parse(await _settingsService.GetSettingAsync("NotificationsEnabled", "True"));
                TestRecipientEmail = SmtpSenderEmail;

                await AcademicVM.LoadDataAsync();
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

                await _auditService.LogActivityAsync(
                    SessionManager.CurrentUsername ?? "ADMIN-01",
                    "UPDATE",
                    "SMTP Configuration",
                    $"Updated SMTP settings: Host='{SmtpHost}', Port='{SmtpPort}', Sender='{SmtpSenderEmail}', SSL={SmtpEnableSsl}.");

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

        private void BrowseLoginLogo()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select School Seal / Login Logo",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.ico)|*.png;*.jpg;*.jpeg;*.webp;*.ico|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                LoginLogoPath = dlg.FileName;
            }
        }

        private void ResetLoginLogo()
        {
            LoginLogoPath = string.Empty;
        }

        private void BrowseDashboardLogo()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Dashboard / Sidebar Logo",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.ico)|*.png;*.jpg;*.jpeg;*.webp;*.ico|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                DashboardLogoPath = dlg.FileName;
            }
        }

        private void ResetDashboardLogo()
        {
            DashboardLogoPath = string.Empty;
        }

        private static object? LoadImagePreview(string? path, string defaultUri)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                else if (!string.IsNullOrWhiteSpace(defaultUri) && defaultUri.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(defaultUri, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch { }
            return null;
        }

        private async Task SaveBrandingAsync()
        {
            try
            {
                string brandingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "Branding");
                Directory.CreateDirectory(brandingDir);

                string finalLoginPath = LoginLogoPath ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(LoginLogoPath) && File.Exists(LoginLogoPath) && !LoginLogoPath.StartsWith(brandingDir, StringComparison.OrdinalIgnoreCase))
                {
                    string ext = Path.GetExtension(LoginLogoPath);
                    string destFile = Path.Combine(brandingDir, $"login_seal_{Guid.NewGuid().ToString("N")[..6]}{ext}");
                    File.Copy(LoginLogoPath, destFile, true);
                    finalLoginPath = destFile;
                    LoginLogoPath = destFile;
                }

                string finalDashPath = UseSameLogoForDashboard ? string.Empty : (DashboardLogoPath ?? string.Empty);
                if (!UseSameLogoForDashboard && !string.IsNullOrWhiteSpace(DashboardLogoPath) && File.Exists(DashboardLogoPath) && !DashboardLogoPath.StartsWith(brandingDir, StringComparison.OrdinalIgnoreCase))
                {
                    string ext = Path.GetExtension(DashboardLogoPath);
                    string destFile = Path.Combine(brandingDir, $"dash_logo_{Guid.NewGuid().ToString("N")[..6]}{ext}");
                    File.Copy(DashboardLogoPath, destFile, true);
                    finalDashPath = destFile;
                    DashboardLogoPath = destFile;
                }

                await _settingsService.SetSettingAsync("SchoolName", SchoolName ?? string.Empty, "School Name");
                await _settingsService.SetSettingAsync("SchoolAddress", SchoolAddress ?? string.Empty, "School Address / District");
                await _settingsService.SetSettingAsync("SystemTitle", SystemTitle ?? string.Empty, "System Application Title");
                await _settingsService.SetSettingAsync("SystemAcronym", SystemAcronym ?? string.Empty, "System Acronym / Short Name");
                await _settingsService.SetSettingAsync("Motto", Motto ?? string.Empty, "School Motto");
                await _settingsService.SetSettingAsync("Tagline", Tagline ?? string.Empty, "System Tagline");
                await _settingsService.SetSettingAsync("LoginLogoPath", finalLoginPath, "Custom Login School Seal Path");
                await _settingsService.SetSettingAsync("DashboardLogoPath", finalDashPath, "Custom Dashboard Logo Path");

                await _auditService.LogActivityAsync(
                    SessionManager.CurrentUsername ?? "ADMIN-01",
                    "UPDATE",
                    "School Branding",
                    $"Updated school branding: SchoolName='{SchoolName}', SystemTitle='{SystemTitle}', Motto='{Motto}'.");

                BrandingStatusMessage = "School branding and logos updated successfully across the system.";
                MessageBox.Show("Branding changes and logos saved successfully across the system.", "Branding Saved", MessageBoxButton.OK, MessageBoxImage.Information);
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

                await _auditService.LogActivityAsync(
                    SessionManager.CurrentUsername ?? "ADMIN-01",
                    "BACKUP",
                    "Database Maintenance",
                    $"Created database backup snapshot: {Path.GetFileName(backupFile)}");

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
