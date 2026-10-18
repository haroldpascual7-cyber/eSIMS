using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Helpers;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class StudentsViewModel : ObservableObject
    {
        private readonly IStudentService _studentService;
        private readonly IIncidentService _incidentService;
        private readonly Action<string> _openCreateIncidentAction;

        private Student? _selectedStudent;
        private string _searchText = string.Empty;
        private string _selectedGradeFilter = "All Grades";
        private string _selectedSectionFilter = "All Sections";
        private bool _isLoading;

        // --- Single Unified Student Modal State (Create / View / Edit) ---
        private bool _isStudentModalOpen;
        private bool _isModalEditing;
        private bool _isModalNewRecord;
        private string _modalTitle = "Student Details";
        private string _modalErrorMessage = string.Empty;
        private int _modalStudentPk;

        // Form Fields
        private string _modalStudentNumber = string.Empty;
        private string _modalFirstName = string.Empty;
        private string _modalMiddleName = string.Empty;
        private string _modalLastName = string.Empty;
        private string _modalGender = "Male";
        private DateTime? _modalDateOfBirth = DateTime.Today.AddYears(-15);
        private string _modalEmail = string.Empty;
        private string _modalPhone = string.Empty;
        private string _modalAddress = string.Empty;
        private string _modalGradeLevel = "Grade 10";
        private string _modalSection = "Emerald";
        private string _modalTrackStrand = "Academic";
        private string _modalAdviser = string.Empty;
        private string _modalGuardianName = string.Empty;
        private string _modalGuardianRelation = "Parent";
        private string _modalGuardianContact = string.Empty;
        private string _modalGuardianEmail = string.Empty;
        private string _modalMedicalNotes = string.Empty;

        public StudentsViewModel(
            IStudentService studentService,
            IIncidentService incidentService,
            Action<string> openCreateIncidentAction)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _openCreateIncidentAction = openCreateIncidentAction ?? throw new ArgumentNullException(nameof(openCreateIncidentAction));

            Students = new ObservableCollection<Student>();

            GradeFilterOptions = new ObservableCollection<string>
            {
                "All Grades", "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12"
            };

            SectionFilterOptions = new ObservableCollection<string>
            {
                "All Sections", "Emerald", "Diamond", "Ruby", "Sapphire", "Pearl", "Topaz"
            };

            GenderOptions = new ObservableCollection<string> { "Male", "Female" };
            GradeOptions = new ObservableCollection<string> { "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12" };

            // CRUD Commands
            OpenAddStudentCommand = new RelayCommand(OpenAddStudentModal);
            OpenEditStudentCommand = new RelayCommand(param =>
            {
                if (param is Student s) OpenEditStudentModal(s);
                else if (SelectedStudent != null) OpenEditStudentModal(SelectedStudent);
            });
            ViewStudentDetailsCommand = new RelayCommand(param =>
            {
                if (param is Student s) OpenViewStudentModal(s);
                else if (SelectedStudent != null) OpenViewStudentModal(SelectedStudent);
            });
            DeleteStudentCommand = new RelayCommand(async param =>
            {
                if (param is Student s) await DeleteStudentAsync(s);
                else if (SelectedStudent != null) await DeleteStudentAsync(SelectedStudent);
            });

            CreateIncidentCommand = new RelayCommand(param =>
            {
                string targetStudent = (param as Student)?.FullName ?? SelectedStudent?.FullName ?? string.Empty;
                CloseAllModals();
                _openCreateIncidentAction(targetStudent);
            });

            // Modal Commands
            SaveStudentCommand = new RelayCommand(async () => await SaveStudentModalAsync());
            CancelModalCommand = new RelayCommand(() => IsStudentModalOpen = false);
            ToggleEditModeCommand = new RelayCommand(() => IsModalEditing = !IsModalEditing);
            CancelEditModeCommand = new RelayCommand(() =>
            {
                if (IsModalNewRecord) IsStudentModalOpen = false;
                else if (SelectedStudent != null) OpenViewStudentModal(SelectedStudent);
            });
            DeleteCurrentStudentCommand = new RelayCommand(async () =>
            {
                if (SelectedStudent != null)
                {
                    await DeleteStudentAsync(SelectedStudent);
                    IsStudentModalOpen = false;
                }
            }, () => CanDeleteCurrentStudent);
            ViewCurrentStudentDossierCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null)
                {
                    IsStudentModalOpen = false;
                    OpenStudentProfileModal(SelectedStudent);
                }
            });
            CreateIncidentForCurrentStudentCommand = new RelayCommand(() =>
            {
                string target = SelectedStudent?.FullName ?? string.Empty;
                CloseAllModals();
                _openCreateIncidentAction(target);
            });

            // Import & Profile Commands
            InitializeImportAndProfileCommands();
            InitializeDetailCommands();

            // Filter Commands
            ApplyFiltersCommand = new RelayCommand(async () => await LoadStudentsAsync());
            ClearFiltersCommand = new RelayCommand(async () =>
            {
                SearchText = string.Empty;
                SelectedGradeFilter = "All Grades";
                SelectedSectionFilter = "All Sections";
                await LoadStudentsAsync();
            });
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshAsync(), () => !IsLoading && !IsRefreshing);

            _ = LoadStudentsAsync();
        }

        // --- Data Collections ---
        public ObservableCollection<Student> Students { get; }
        public ObservableCollection<string> GradeFilterOptions { get; }
        public ObservableCollection<string> SectionFilterOptions { get; }
        public ObservableCollection<string> GenderOptions { get; }
        public ObservableCollection<string> GradeOptions { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonIcon));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadStudentsAsync();
                }
            }
        }

        public string SelectedGradeFilter
        {
            get => _selectedGradeFilter;
            set
            {
                if (SetProperty(ref _selectedGradeFilter, value))
                {
                    _ = LoadStudentsAsync();
                }
            }
        }

        public string SelectedSectionFilter
        {
            get => _selectedSectionFilter;
            set
            {
                if (SetProperty(ref _selectedSectionFilter, value))
                {
                    _ = LoadStudentsAsync();
                }
            }
        }

        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set => SetProperty(ref _selectedStudent, value);
        }

        // Permissions
        public bool CanManageStudents => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");
        public bool CanDeleteStudent => SessionManager.HasPermission("System Administrator,Discipline Officer");
        public bool CanDeleteCurrentStudent => !IsModalNewRecord && CanDeleteStudent;

        // --- Student Modal Properties ---
        public bool IsStudentModalOpen { get => _isStudentModalOpen; set => SetProperty(ref _isStudentModalOpen, value); }
        public bool IsModalEditing { get => _isModalEditing; set { SetProperty(ref _isModalEditing, value); OnPropertyChanged(nameof(IsModalReadOnly)); } }
        public bool IsModalReadOnly => !IsModalEditing;
        public bool IsModalNewRecord { get => _isModalNewRecord; set => SetProperty(ref _isModalNewRecord, value); }
        public string ModalTitle { get => _modalTitle; set => SetProperty(ref _modalTitle, value); }
        public string ModalErrorMessage { get => _modalErrorMessage; set => SetProperty(ref _modalErrorMessage, value); }

        public string ModalStudentNumber { get => _modalStudentNumber; set => SetProperty(ref _modalStudentNumber, value); }
        public string ModalFirstName { get => _modalFirstName; set => SetProperty(ref _modalFirstName, value); }
        public string ModalMiddleName { get => _modalMiddleName; set => SetProperty(ref _modalMiddleName, value); }
        public string ModalLastName { get => _modalLastName; set => SetProperty(ref _modalLastName, value); }
        public string ModalGender { get => _modalGender; set => SetProperty(ref _modalGender, value); }
        public DateTime? ModalDateOfBirth { get => _modalDateOfBirth; set => SetProperty(ref _modalDateOfBirth, value); }
        public string ModalEmail { get => _modalEmail; set => SetProperty(ref _modalEmail, value); }
        public string ModalPhone { get => _modalPhone; set => SetProperty(ref _modalPhone, value); }
        public string ModalAddress { get => _modalAddress; set => SetProperty(ref _modalAddress, value); }
        public string ModalGradeLevel { get => _modalGradeLevel; set => SetProperty(ref _modalGradeLevel, value); }
        public string ModalSection { get => _modalSection; set => SetProperty(ref _modalSection, value); }
        public string ModalTrackStrand { get => _modalTrackStrand; set => SetProperty(ref _modalTrackStrand, value); }
        public string ModalAdviser { get => _modalAdviser; set => SetProperty(ref _modalAdviser, value); }
        public string ModalGuardianName { get => _modalGuardianName; set => SetProperty(ref _modalGuardianName, value); }
        public string ModalGuardianRelation { get => _modalGuardianRelation; set => SetProperty(ref _modalGuardianRelation, value); }
        public string ModalGuardianContact { get => _modalGuardianContact; set => SetProperty(ref _modalGuardianContact, value); }
        public string ModalGuardianEmail { get => _modalGuardianEmail; set => SetProperty(ref _modalGuardianEmail, value); }
        public string ModalMedicalNotes { get => _modalMedicalNotes; set => SetProperty(ref _modalMedicalNotes, value); }

        // --- Commands ---
        public ICommand OpenAddStudentCommand { get; }
        public ICommand OpenEditStudentCommand { get; }
        public ICommand ViewStudentDetailsCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand CreateIncidentCommand { get; }
        public ICommand SaveStudentCommand { get; }
        public ICommand CancelModalCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        public ICommand CancelEditModeCommand { get; }
        public ICommand DeleteCurrentStudentCommand { get; }
        public ICommand ViewCurrentStudentDossierCommand { get; }
        public ICommand CreateIncidentForCurrentStudentCommand { get; }

        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task LoadStudentsAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _studentService.SearchStudentsAsync(SearchText, SelectedGradeFilter, SelectedSectionFilter);
                Students.Clear();
                foreach (var s in list)
                {
                    Students.Add(s);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading students: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenAddStudentModal()
        {
            ModalErrorMessage = string.Empty;
            IsModalNewRecord = true;
            IsModalEditing = true;
            ModalTitle = "Add New Student";
            _modalStudentPk = 0;

            ModalStudentNumber = $"STD-{DateTime.Now:yyyy}-{new Random().Next(1000, 9999)}";
            ModalFirstName = string.Empty;
            ModalMiddleName = string.Empty;
            ModalLastName = string.Empty;
            ModalGender = "Male";
            ModalDateOfBirth = DateTime.Today.AddYears(-15);
            ModalEmail = string.Empty;
            ModalPhone = string.Empty;
            ModalAddress = string.Empty;
            ModalGradeLevel = "Grade 10";
            ModalSection = "Emerald";
            ModalTrackStrand = "Academic";
            ModalAdviser = string.Empty;
            ModalGuardianName = string.Empty;
            ModalGuardianRelation = "Parent";
            ModalGuardianContact = string.Empty;
            ModalGuardianEmail = string.Empty;
            ModalMedicalNotes = string.Empty;

            IsStudentModalOpen = true;
        }

        private void OpenViewStudentModal(Student s)
        {
            SelectedStudent = s;
            ModalErrorMessage = string.Empty;
            IsModalNewRecord = false;
            IsModalEditing = false;
            ModalTitle = $"Student Details: {s.FullName} ({s.StudentNumber})";
            _modalStudentPk = s.Id;

            ModalStudentNumber = s.StudentNumber;
            ModalFirstName = s.FirstName;
            ModalMiddleName = s.MiddleName ?? string.Empty;
            ModalLastName = s.LastName;
            ModalGender = s.Gender;
            ModalDateOfBirth = s.DateOfBirth;
            ModalEmail = s.Email;
            ModalPhone = s.Phone;
            ModalAddress = s.Address;
            ModalGradeLevel = s.GradeLevel;
            ModalSection = s.Section;
            ModalTrackStrand = s.TrackStrand;
            ModalAdviser = s.Adviser;
            ModalGuardianName = s.GuardianName;
            ModalGuardianRelation = s.GuardianRelation;
            ModalGuardianContact = s.GuardianContact;
            ModalGuardianEmail = s.GuardianEmail;
            ModalMedicalNotes = s.MedicalNotes;

            LoadStudentDetailStatistics(s);
            IsStudentDetailOpen = true;
            IsStudentModalOpen = false;
        }

        private void OpenEditStudentModal(Student s)
        {
            OpenViewStudentModal(s);
            IsModalEditing = true;
            ModalTitle = $"Edit Student: {s.FullName} ({s.StudentNumber})";
        }

        private async Task SaveStudentModalAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalStudentNumber))
            {
                ModalErrorMessage = "Student ID number is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ModalFirstName) || string.IsNullOrWhiteSpace(ModalLastName))
            {
                ModalErrorMessage = "First and last names are required.";
                return;
            }

            try
            {
                if (IsModalNewRecord)
                {
                    var student = new Student
                    {
                        StudentNumber = ModalStudentNumber.Trim(),
                        FirstName = ModalFirstName.Trim(),
                        MiddleName = ModalMiddleName?.Trim() ?? string.Empty,
                        LastName = ModalLastName.Trim(),
                        Gender = ModalGender,
                        DateOfBirth = ModalDateOfBirth,
                        Email = ModalEmail?.Trim() ?? string.Empty,
                        Phone = ModalPhone?.Trim() ?? string.Empty,
                        Address = ModalAddress?.Trim() ?? string.Empty,
                        GradeLevel = ModalGradeLevel,
                        Section = ModalSection?.Trim() ?? string.Empty,
                        TrackStrand = ModalTrackStrand,
                        Adviser = ModalAdviser?.Trim() ?? string.Empty,
                        GuardianName = ModalGuardianName?.Trim() ?? string.Empty,
                        GuardianRelation = ModalGuardianRelation,
                        GuardianContact = ModalGuardianContact?.Trim() ?? string.Empty,
                        GuardianEmail = ModalGuardianEmail?.Trim() ?? string.Empty,
                        MedicalNotes = ModalMedicalNotes?.Trim() ?? string.Empty
                    };

                    await _studentService.AddStudentAsync(student);
                    await LoadStudentsAsync();
                    var newStudent = Students.FirstOrDefault(x => x.StudentNumber == student.StudentNumber) ?? student;
                    SelectedStudent = null;
                    OpenViewStudentModal(newStudent);
                    OnPropertyChanged(nameof(SelectedStudent));
                    OnPropertyChanged(nameof(DetailLevelCategory));
                    MessageBox.Show($"Student record for '{newStudent.FullName}' created successfully!", "Student Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var student = await _studentService.GetStudentByIdAsync(_modalStudentPk);
                    if (student != null)
                    {
                        student.StudentNumber = ModalStudentNumber.Trim();
                        student.FirstName = ModalFirstName.Trim();
                        student.MiddleName = ModalMiddleName?.Trim() ?? string.Empty;
                        student.LastName = ModalLastName.Trim();
                        student.Gender = ModalGender;
                        student.DateOfBirth = ModalDateOfBirth;
                        student.Email = ModalEmail?.Trim() ?? string.Empty;
                        student.Phone = ModalPhone?.Trim() ?? string.Empty;
                        student.Address = ModalAddress?.Trim() ?? string.Empty;
                        student.GradeLevel = ModalGradeLevel;
                        student.Section = ModalSection?.Trim() ?? string.Empty;
                        student.TrackStrand = ModalTrackStrand;
                        student.Adviser = ModalAdviser?.Trim() ?? string.Empty;
                        student.GuardianName = ModalGuardianName?.Trim() ?? string.Empty;
                        student.GuardianRelation = ModalGuardianRelation;
                        student.GuardianContact = ModalGuardianContact?.Trim() ?? string.Empty;
                        student.GuardianEmail = ModalGuardianEmail?.Trim() ?? string.Empty;
                        student.MedicalNotes = ModalMedicalNotes?.Trim() ?? string.Empty;

                        await _studentService.UpdateStudentAsync(student);
                        await LoadStudentsAsync();

                        var updatedStudent = Students.FirstOrDefault(x => x.Id == student.Id) ?? student;
                        SelectedStudent = null;
                        OpenViewStudentModal(updatedStudent);
                        OnPropertyChanged(nameof(SelectedStudent));
                        OnPropertyChanged(nameof(DetailLevelCategory));

                        MessageBox.Show($"Student information for '{updatedStudent.FullName}' updated successfully!", "Update Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = ex.Message;
                MessageBox.Show($"Error saving student record: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteStudentAsync(Student s)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete student record '{s.FullName}' ({s.StudentNumber})?\n\nThis will permanently remove the student from the database.",
                "Confirm Student Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _studentService.DeleteStudentAsync(s.Id);
                await LoadStudentsAsync();
            }
        }
    }
}
