using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Helpers;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class StudentsViewModel
    {
        // --- Import Modal State ---
        private bool _isImportModalOpen;
        private string _importFilePath = string.Empty;
        private string _importStatusMessage = string.Empty;
        private bool _isImporting;
        private ObservableCollection<StudentImportRow> _importPreviewRows = new();

        // --- Student Profile Dossier Modal State ---
        private bool _isProfileModalOpen;
        private ObservableCollection<Incident> _studentIncidents = new();
        private ObservableCollection<StudentMeritLog> _studentMerits = new();

        public bool IsImportModalOpen { get => _isImportModalOpen; set => SetProperty(ref _isImportModalOpen, value); }
        public string ImportFilePath { get => _importFilePath; set => SetProperty(ref _importFilePath, value); }
        public string ImportStatusMessage { get => _importStatusMessage; set => SetProperty(ref _importStatusMessage, value); }
        public bool IsImporting { get => _isImporting; set => SetProperty(ref _isImporting, value); }
        public ObservableCollection<StudentImportRow> ImportPreviewRows => _importPreviewRows;
        public int ValidImportCount => _importPreviewRows.Count(r => r.IsValid);
        public int InvalidImportCount => _importPreviewRows.Count(r => !r.IsValid);

        public bool IsProfileModalOpen { get => _isProfileModalOpen; set => SetProperty(ref _isProfileModalOpen, value); }
        public ObservableCollection<Incident> StudentIncidents => _studentIncidents;
        public ObservableCollection<StudentMeritLog> StudentMerits => _studentMerits;

        public ICommand OpenImportModalCommand { get; private set; } = null!;
        public ICommand CloseImportModalCommand { get; private set; } = null!;
        public ICommand SelectImportFileCommand { get; private set; } = null!;
        public ICommand ConfirmImportCommand { get; private set; } = null!;
        public ICommand CloseProfileModalCommand { get; private set; } = null!;

        private void InitializeImportAndProfileCommands()
        {
            OpenImportModalCommand = new RelayCommand(OpenImportModal);
            CloseImportModalCommand = new RelayCommand(() => IsImportModalOpen = false);
            SelectImportFileCommand = new RelayCommand(SelectImportFile);
            ConfirmImportCommand = new RelayCommand(async () => await ConfirmImportAsync(), () => ValidImportCount > 0 && !IsImporting);
            CloseProfileModalCommand = new RelayCommand(() => IsProfileModalOpen = false);
        }

        private void CloseAllModals()
        {
            IsStudentModalOpen = false;
            IsImportModalOpen = false;
            IsProfileModalOpen = false;
        }

        private void OpenStudentProfileModal(Student s)
        {
            SelectedStudent = s;
            _studentIncidents.Clear();
            _studentMerits.Clear();

            try
            {
                _ = Task.Run(async () =>
                {
                    var allIncidents = await _incidentService.GetAllIncidentsAsync();
                    var related = allIncidents.Where(i => i.StudentId == s.Id ||
                                                         (i.PrimaryInvolvedStudent != null && i.PrimaryInvolvedStudent.Contains(s.FullName, StringComparison.OrdinalIgnoreCase)))
                                              .OrderByDescending(i => i.IncidentDate)
                                              .ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var inc in related) _studentIncidents.Add(inc);
                    });
                });
            }
            catch { }

            IsProfileModalOpen = true;
        }

        private void OpenImportModal()
        {
            ImportFilePath = string.Empty;
            ImportStatusMessage = string.Empty;
            _importPreviewRows.Clear();
            OnPropertyChanged(nameof(ValidImportCount));
            OnPropertyChanged(nameof(InvalidImportCount));
            IsImportModalOpen = true;
        }

        private void SelectImportFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel or CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Workbooks (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                Title = "Select Student Roster File to Import"
            };

            if (dlg.ShowDialog() == true)
            {
                ImportFilePath = dlg.FileName;
                ParseImportFile(dlg.FileName);
            }
        }

        private async void ParseImportFile(string filePath)
        {
            try
            {
                var existingStudents = await _studentService.GetAllStudentsAsync();
                var existingIds = existingStudents.Select(s => s.StudentNumber.ToLowerInvariant()).ToHashSet();

                var rows = StudentImportHelper.ParseFile(filePath, existingIds);
                _importPreviewRows.Clear();
                foreach (var r in rows)
                {
                    _importPreviewRows.Add(r);
                }

                OnPropertyChanged(nameof(ValidImportCount));
                OnPropertyChanged(nameof(InvalidImportCount));

                ImportStatusMessage = $"Loaded {rows.Count} rows from file. ({ValidImportCount} valid, {InvalidImportCount} invalid)";
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"Error parsing file: {ex.Message}";
            }
        }

        private async Task ConfirmImportAsync()
        {
            var validRows = _importPreviewRows.Where(r => r.IsValid).ToList();
            if (validRows.Count == 0)
            {
                MessageBox.Show("There are no valid student rows to import.", "Import Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsImporting = true;
            try
            {
                var studentsToImport = validRows.Select(r => r.ToStudent()).ToList();
                var result = await _studentService.ImportStudentsBatchAsync(studentsToImport);

                MessageBox.Show(
                    $"Import Completed!\n\nSuccessfully Imported: {result.successCount}\nFailed: {result.failureCount}",
                    "Student Roster Import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                IsImportModalOpen = false;
                await LoadStudentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import error: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsImporting = false;
            }
        }
    }
}
