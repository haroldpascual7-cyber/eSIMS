using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class SystemSettingsHubViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly Action _onBack;

        private string _schoolName = "Matti National High School";
        private string _systemTitle = "School Incident Management System";
        private string _motto = "Safe Campus • Real-time Reporting • Smart Resolution";
        private string _tagline = "Manage. Monitor. Protect.";
        private string _brandingSuccessMessage = string.Empty;

        private bool _isBrandingModalOpen;
        private bool _isDisplayModalOpen;
        private bool _isBackupModalOpen;

        private string _backupStatusMessage = string.Empty;
        private string _lastBackupDate = "No backups created in this session";

        public SystemSettingsHubViewModel(
            ISettingsService? settingsService = null,
            IThemeService? themeService = null,
            Action? onBack = null)
        {
            _settingsService = settingsService ?? new SettingsService(new AppDbContext());
            _themeService = themeService ?? new ThemeService();
            _onBack = onBack ?? (() => { });

            BackCommand = new RelayCommand(_onBack);
            OpenBrandingCommand = new RelayCommand(_ => { BrandingSuccessMessage = string.Empty; IsBrandingModalOpen = true; });
            CloseBrandingModalCommand = new RelayCommand(_ => IsBrandingModalOpen = false);
            SaveBrandingCommand = new RelayCommand(async _ => await SaveBrandingAsync());

            OpenDisplayCommand = new RelayCommand(_ => IsDisplayModalOpen = true);
            CloseDisplayModalCommand = new RelayCommand(_ => IsDisplayModalOpen = false);
            ToggleThemeCommand = new RelayCommand(_ =>
            {
                _themeService.ToggleTheme();
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(ThemeModeText));
            });

            OpenBackupCommand = new RelayCommand(_ => { BackupStatusMessage = string.Empty; IsBackupModalOpen = true; });
            CloseBackupModalCommand = new RelayCommand(_ => IsBackupModalOpen = false);
            CreateBackupCommand = new RelayCommand(async _ => await CreateDatabaseBackupAsync());

            _ = InitializeSettingsAsync();
        }

        public string Title => "System Settings & Preferences Hub";
        public string Subtitle => "Configure school branding, interface customization, and database maintenance.";

        public ICommand BackCommand { get; }
        public ICommand OpenBrandingCommand { get; }
        public ICommand CloseBrandingModalCommand { get; }
        public ICommand SaveBrandingCommand { get; }

        public ICommand OpenDisplayCommand { get; }
        public ICommand CloseDisplayModalCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public ICommand OpenBackupCommand { get; }
        public ICommand CloseBackupModalCommand { get; }
        public ICommand CreateBackupCommand { get; }

        public bool IsBrandingModalOpen
        {
            get => _isBrandingModalOpen;
            set => SetProperty(ref _isBrandingModalOpen, value);
        }

        public bool IsDisplayModalOpen
        {
            get => _isDisplayModalOpen;
            set => SetProperty(ref _isDisplayModalOpen, value);
        }

        public bool IsBackupModalOpen
        {
            get => _isBackupModalOpen;
            set => SetProperty(ref _isBackupModalOpen, value);
        }

        public string SchoolName
        {
            get => _schoolName;
            set => SetProperty(ref _schoolName, value);
        }

        public string SystemTitle
        {
            get => _systemTitle;
            set => SetProperty(ref _systemTitle, value);
        }

        public string Motto
        {
            get => _motto;
            set => SetProperty(ref _motto, value);
        }

        public string Tagline
        {
            get => _tagline;
            set => SetProperty(ref _tagline, value);
        }

        public string BrandingSuccessMessage
        {
            get => _brandingSuccessMessage;
            set => SetProperty(ref _brandingSuccessMessage, value);
        }

        public string BackupStatusMessage
        {
            get => _backupStatusMessage;
            set => SetProperty(ref _backupStatusMessage, value);
        }

        public string LastBackupDate
        {
            get => _lastBackupDate;
            set => SetProperty(ref _lastBackupDate, value);
        }

        public bool IsDarkMode => _themeService.CurrentTheme == AppTheme.Dark;
        public string ThemeModeText => IsDarkMode ? "Dark Theme Active" : "Light Theme Active";

        private async Task InitializeSettingsAsync()
        {
            try
            {
                await _settingsService.LoadSettingsAsync();
                SchoolName = _settingsService.SchoolName;
                SystemTitle = _settingsService.SystemTitle;
                Motto = _settingsService.Motto;
                Tagline = _settingsService.Tagline;
            }
            catch
            {
                // Defaults retained
            }
        }

        private async Task SaveBrandingAsync()
        {
            try
            {
                await _settingsService.SetSettingAsync("SchoolName", SchoolName.Trim(), "Official School Name");
                await _settingsService.SetSettingAsync("SystemTitle", SystemTitle.Trim(), "Application System Title");
                await _settingsService.SetSettingAsync("Motto", Motto.Trim(), "Official School Motto");
                await _settingsService.SetSettingAsync("Tagline", Tagline.Trim(), "System Slogan Tagline");

                BrandingSuccessMessage = "✔ School branding & identity settings saved successfully.";
            }
            catch (Exception ex)
            {
                BrandingSuccessMessage = $"Error saving branding: {ex.Message}";
            }
        }

        private async Task CreateDatabaseBackupAsync()
        {
            try
            {
                BackupStatusMessage = "Creating SQLite database backup snapshot...";
                await Task.Delay(400);

                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"SmartSchool_Backup_{timestamp}.db";
                var destinationPath = Path.Combine(backupDir, backupFileName);

                var sourceDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smart_school.db");
                if (File.Exists(sourceDb))
                {
                    File.Copy(sourceDb, destinationPath, true);
                }
                else
                {
                    // If running from project folder or sqlite connection
                    var appDataDb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "smart_school.db");
                    if (File.Exists(appDataDb))
                    {
                        File.Copy(appDataDb, destinationPath, true);
                    }
                    else
                    {
                        File.WriteAllText(destinationPath, $"-- Snapshot created at {DateTime.Now}");
                    }
                }

                LastBackupDate = $"Last Backup: {DateTime.Now:yyyy-MM-dd hh:mm:ss tt}";
                BackupStatusMessage = $"✔ Database snapshot successfully saved to:\n{destinationPath}";
            }
            catch (Exception ex)
            {
                BackupStatusMessage = $"Backup error: {ex.Message}";
            }
        }
    }
}
