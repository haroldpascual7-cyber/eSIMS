using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class AcademicStructureViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IAuditService _auditService;

        private string _newGradeLevel = string.Empty;
        private string _newSection = string.Empty;
        private string _newDepartment = string.Empty;
        private string _statusMessage = string.Empty;

        public ObservableCollection<string> GradeLevels { get; } = new();
        public ObservableCollection<string> Sections { get; } = new();
        public ObservableCollection<string> Departments { get; } = new();

        public string NewGradeLevel
        {
            get => _newGradeLevel;
            set => SetProperty(ref _newGradeLevel, value);
        }

        public string NewSection
        {
            get => _newSection;
            set => SetProperty(ref _newSection, value);
        }

        public string NewDepartment
        {
            get => _newDepartment;
            set => SetProperty(ref _newDepartment, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand AddGradeLevelCommand { get; }
        public ICommand RemoveGradeLevelCommand { get; }
        public ICommand AddSectionCommand { get; }
        public ICommand RemoveSectionCommand { get; }
        public ICommand AddDepartmentCommand { get; }
        public ICommand RemoveDepartmentCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }

        public AcademicStructureViewModel(ISettingsService settingsService, IAuditService auditService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

            AddGradeLevelCommand = new RelayCommand(AddGradeLevel);
            RemoveGradeLevelCommand = new RelayCommand(param => RemoveGradeLevel(param as string));
            AddSectionCommand = new RelayCommand(AddSection);
            RemoveSectionCommand = new RelayCommand(param => RemoveSection(param as string));
            AddDepartmentCommand = new RelayCommand(AddDepartment);
            RemoveDepartmentCommand = new RelayCommand(param => RemoveDepartment(param as string));
            SaveChangesCommand = new RelayCommand(async _ => await SaveChangesAsync());
            ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);

            _settingsService.SettingsChanged += () => { _ = LoadDataAsync(); };
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var grades = await _settingsService.GetAllDiscoveredGradeLevelsAsync();
                GradeLevels.Clear();
                foreach (var g in grades) GradeLevels.Add(g);

                var secs = await _settingsService.GetAllDiscoveredSectionsAsync();
                Sections.Clear();
                foreach (var s in secs) Sections.Add(s);

                var depts = await _settingsService.GetAllDiscoveredDepartmentsAsync();
                Departments.Clear();
                foreach (var d in depts) Departments.Add(d);
            }
            catch
            {
                LoadDataFallback();
            }
        }

        public void LoadDataFallback()
        {
            GradeLevels.Clear();
            var grades = (_settingsService.MasterGradeLevels ?? SettingsService.DefaultGradeLevels)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim());
            foreach (var g in grades) GradeLevels.Add(g);

            Sections.Clear();
            var secs = (_settingsService.MasterSections ?? SettingsService.DefaultSections)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());
            foreach (var s in secs) Sections.Add(s);

            Departments.Clear();
            var depts = (_settingsService.MasterDepartments ?? SettingsService.DefaultDepartments)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim());
            foreach (var d in depts) Departments.Add(d);
        }

        private void AddGradeLevel()
        {
            if (string.IsNullOrWhiteSpace(NewGradeLevel)) return;
            string val = NewGradeLevel.Trim();
            if (!GradeLevels.Contains(val, StringComparer.OrdinalIgnoreCase))
            {
                GradeLevels.Add(val);
                NewGradeLevel = string.Empty;
                StatusMessage = $"Added Grade Level '{val}'. Remember to click Save Changes.";
            }
        }

        private void RemoveGradeLevel(string? level)
        {
            if (!string.IsNullOrWhiteSpace(level) && GradeLevels.Contains(level))
            {
                GradeLevels.Remove(level);
                StatusMessage = $"Removed '{level}'. Remember to click Save Changes.";
            }
        }

        private void AddSection()
        {
            if (string.IsNullOrWhiteSpace(NewSection)) return;
            string val = NewSection.Trim();
            if (!Sections.Contains(val, StringComparer.OrdinalIgnoreCase))
            {
                Sections.Add(val);
                NewSection = string.Empty;
                StatusMessage = $"Added Section '{val}'. Remember to click Save Changes.";
            }
        }

        private void RemoveSection(string? sec)
        {
            if (!string.IsNullOrWhiteSpace(sec) && Sections.Contains(sec))
            {
                Sections.Remove(sec);
                StatusMessage = $"Removed '{sec}'. Remember to click Save Changes.";
            }
        }

        private void AddDepartment()
        {
            if (string.IsNullOrWhiteSpace(NewDepartment)) return;
            string val = NewDepartment.Trim();
            if (!Departments.Contains(val, StringComparer.OrdinalIgnoreCase))
            {
                Departments.Add(val);
                NewDepartment = string.Empty;
                StatusMessage = $"Added Department '{val}'. Remember to click Save Changes.";
            }
        }

        private void RemoveDepartment(string? dept)
        {
            if (!string.IsNullOrWhiteSpace(dept) && Departments.Contains(dept))
            {
                Departments.Remove(dept);
                StatusMessage = $"Removed '{dept}'. Remember to click Save Changes.";
            }
        }

        private void ResetToDefaults()
        {
            GradeLevels.Clear();
            foreach (var g in SettingsService.DefaultGradeLevels.Split(',').Select(x => x.Trim())) GradeLevels.Add(g);

            Sections.Clear();
            foreach (var s in SettingsService.DefaultSections.Split(',').Select(x => x.Trim())) Sections.Add(s);

            Departments.Clear();
            foreach (var d in SettingsService.DefaultDepartments.Split(',').Select(x => x.Trim())) Departments.Add(d);

            StatusMessage = "Reverted academic structures to DepEd defaults. Click Save Changes to apply.";
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                string gradeStr = string.Join(", ", GradeLevels);
                string sectionStr = string.Join(", ", Sections);
                string deptStr = string.Join(", ", Departments);

                await _settingsService.SetSettingAsync("MasterGradeLevels", gradeStr, "Official School Grade Levels");
                await _settingsService.SetSettingAsync("MasterSections", sectionStr, "Official School Sections");
                await _settingsService.SetSettingAsync("MasterDepartments", deptStr, "Official School Departments");
                await _settingsService.LoadSettingsAsync();

                await _auditService.LogActivityAsync(
                    SessionManager.CurrentUsername ?? "ADMIN-01",
                    "UPDATE",
                    "Academic Configuration",
                    $"Updated academic configuration: {GradeLevels.Count} Grade Levels, {Sections.Count} Sections, {Departments.Count} Departments.");

                StatusMessage = "Academic structure and departments successfully saved and audited.";
                MessageBox.Show("Academic configuration saved successfully across the system.", "Configuration Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show(StatusMessage, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
