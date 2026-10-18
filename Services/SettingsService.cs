using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public const string DefaultSchoolSealUri = "pack://application:,,,/Assets/Matti_National_High_School_logo.png";

        public string SchoolName { get; private set; } = "Matti National High School";
        public string SchoolAddress { get; private set; } = "Digos City, Davao Del Sur";
        public string SystemTitle { get; private set; } = "School Incident Management System";
        public string SystemAcronym { get; private set; } = "eSIMS";
        public string Tagline { get; private set; } = "Campus Safety • Behavioral Intelligence • Student Welfare";
        public string Motto { get; private set; } = "Safe Campus • Real-time Reporting • Smart Resolution";
        public string LogoIcon { get; private set; } = "🛡️";
        public string LoginLogoPath { get; private set; } = string.Empty;
        public string DashboardLogoPath { get; private set; } = string.Empty;

        public const string DefaultGradeLevels = "Grade 7, Grade 8, Grade 9, Grade 10, Grade 11, Grade 12";
        public const string DefaultSections = "Emerald, Diamond, Ruby, Sapphire, Pearl, Topaz, Prudence, Rizal, Bonifacio";
        public const string DefaultDepartments = "Junior High School Faculty, Senior High School Faculty, Guidance & Counseling Office, Prefect of Discipline, Administration & Records, Campus Security";

        public string MasterGradeLevels { get; private set; } = DefaultGradeLevels;
        public string MasterSections { get; private set; } = DefaultSections;
        public string MasterDepartments { get; private set; } = DefaultDepartments;

        public object? LoginLogoSource => LoadImageSource(LoginLogoPath, DefaultSchoolSealUri);
        public object? DashboardLogoSource => LoadImageSource(DashboardLogoPath, LoginLogoPath, DefaultSchoolSealUri);

        public event Action? SettingsChanged;

        public async Task LoadSettingsAsync()
        {
            try
            {
                SchoolName = await GetSettingAsync("SchoolName", "Matti National High School");
                SchoolAddress = await GetSettingAsync("SchoolAddress", "Digos City, Davao Del Sur");
                SystemTitle = await GetSettingAsync("SystemTitle", "School Incident Management System");
                SystemAcronym = await GetSettingAsync("SystemAcronym", "eSIMS");
                Tagline = await GetSettingAsync("Tagline", "Campus Safety • Behavioral Intelligence • Student Welfare");
                Motto = await GetSettingAsync("Motto", "Safe Campus • Real-time Reporting • Smart Resolution");
                LogoIcon = await GetSettingAsync("LogoIcon", "🛡️");
                LoginLogoPath = await GetSettingAsync("LoginLogoPath", string.Empty);
                DashboardLogoPath = await GetSettingAsync("DashboardLogoPath", string.Empty);
                MasterGradeLevels = await GetSettingAsync("MasterGradeLevels", DefaultGradeLevels);
                MasterSections = await GetSettingAsync("MasterSections", DefaultSections);
                MasterDepartments = await GetSettingAsync("MasterDepartments", DefaultDepartments);

                SettingsChanged?.Invoke();
            }
            catch
            {
                // Fallback to defaults if DB is initializing
            }
        }

        private static object? LoadImageSource(params string?[] pathsOrUris)
        {
            foreach (var path in pathsOrUris)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                try
                {
                    if (System.IO.File.Exists(path))
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
                    else if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
                catch { }
            }
            return null;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting != null ? setting.Value : defaultValue;
        }

        public async Task SetSettingAsync(string key, string value, string description = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Description = description
                };
                await _context.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                if (!string.IsNullOrEmpty(description))
                {
                    setting.Description = description;
                }
                setting.MarkUpdated();
            }

            await _context.SaveChangesAsync();
            await LoadSettingsAsync();
        }

        public async Task<System.Collections.Generic.List<string>> GetAllDiscoveredSectionsAsync()
        {
            var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string saved = await GetSettingAsync("MasterSections", DefaultSections);
            foreach (var s in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string t = s.Trim();
                if (!string.IsNullOrEmpty(t)) set.Add(t);
            }

            try
            {
                var existingStudentSections = await _context.Students
                    .Where(st => !string.IsNullOrEmpty(st.Section))
                    .Select(st => st.Section)
                    .Distinct()
                    .ToListAsync();
                foreach (var s in existingStudentSections)
                {
                    string t = s.Trim();
                    if (!string.IsNullOrEmpty(t)) set.Add(t);
                }
            }
            catch { }

            return set.OrderBy(x => x).ToList();
        }

        public async Task<System.Collections.Generic.List<string>> GetAllDiscoveredGradeLevelsAsync()
        {
            var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string saved = await GetSettingAsync("MasterGradeLevels", DefaultGradeLevels);
            foreach (var g in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string t = g.Trim();
                if (!string.IsNullOrEmpty(t)) set.Add(t);
            }

            try
            {
                var existingStudentGrades = await _context.Students
                    .Where(st => !string.IsNullOrEmpty(st.GradeLevel))
                    .Select(st => st.GradeLevel)
                    .Distinct()
                    .ToListAsync();
                foreach (var g in existingStudentGrades)
                {
                    string t = g.Trim();
                    if (!string.IsNullOrEmpty(t)) set.Add(t);
                }
            }
            catch { }

            return set.OrderBy(x => x).ToList();
        }

        public async Task<System.Collections.Generic.List<string>> GetAllDiscoveredDepartmentsAsync()
        {
            var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string saved = await GetSettingAsync("MasterDepartments", DefaultDepartments);
            foreach (var d in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string t = d.Trim();
                if (!string.IsNullOrEmpty(t)) set.Add(t);
            }

            try
            {
                var existingStaffDepts = await _context.StaffMembers
                    .Where(st => !string.IsNullOrEmpty(st.Department))
                    .Select(st => st.Department)
                    .Distinct()
                    .ToListAsync();
                foreach (var d in existingStaffDepts)
                {
                    string t = d.Trim();
                    if (!string.IsNullOrEmpty(t)) set.Add(t);
                }
            }
            catch { }

            return set.OrderBy(x => x).ToList();
        }
    }
}
