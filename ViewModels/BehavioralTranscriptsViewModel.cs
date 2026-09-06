using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class BehavioralTranscriptsViewModel : ObservableObject
    {
        private readonly IBehavioralTranscriptService _service;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private ObservableCollection<string> _studentSuggestions = new();
        private string _selectedStudent = string.Empty;
        private StudentTranscriptResult? _transcriptResult;
        private ObservableCollection<TranscriptEntryModel> _filteredEntries = new();
        private bool _isLoading, _hasResult, _showModal, _showAddStudentModal, _showEditStudentModal;

        // Enrolled Students Directory
        private List<Student> _allEnrolledStudents = new();
        private ObservableCollection<Student> _filteredEnrolledStudents = new();
        private string _enrolledSearchText = string.Empty;

        // Add Merit Modal Fields
        private string _modalCategory = "Peer Leadership";
        private int _modalPoints = 5;
        private DateTime _modalDate = DateTime.Today;
        private string _modalCitation = string.Empty;

        // Register New Student Fields
        private string _newStudentId = string.Empty, _newFirstName = string.Empty, _newLastName = string.Empty;
        private string _newGradeLevel = "Grade 10", _newSection = string.Empty, _newAdviser = string.Empty;
        private string _newGuardianName = string.Empty, _newGuardianContact = string.Empty;

        // Edit Profile Fields
        private string _editStudentId = string.Empty, _editFirstName = string.Empty, _editMiddleName = string.Empty;
        private string _editLastName = string.Empty, _editGender = "Male";
        private DateTime? _editDateOfBirth = DateTime.Today.AddYears(-16);
        private string _editEmail = string.Empty, _editPhone = string.Empty, _editAddress = string.Empty;
        private string _editGradeLevel = "Grade 10", _editSection = string.Empty, _editTrackStrand = "Academic";
        private string _editAdviser = string.Empty, _editGuardianName = string.Empty;
        private string _editGuardianRelation = "Parent", _editGuardianContact = string.Empty;
        private string _editGuardianEmail = string.Empty, _editMedicalNotes = string.Empty, _editProfilePicturePath = string.Empty;

        public BehavioralTranscriptsViewModel(IBehavioralTranscriptService service, Action onBack, IPdfExportService? pdfService = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _pdfService = pdfService ?? new PdfExportService();
            _onBack  = onBack  ?? throw new ArgumentNullException(nameof(onBack));

            var backCmd = new RelayCommand(_ => _onBack());
            BackCommand = backCmd; BackToProfilesHubCommand = backCmd; BackToOverviewCommand = backCmd; BackToHubCommand = backCmd;

            LoadTranscriptCommand        = new RelayCommand(async _ => await LoadTranscriptAsync());
            OpenAddMeritCommand          = new RelayCommand(_ => ShowModal = true);
            CloseModalCommand            = new RelayCommand(_ => ShowModal = false);
            SaveMeritCommand             = new RelayCommand(async _ => await SaveMeritAsync());

            OpenAddStudentModalCommand   = new RelayCommand(_ => ShowAddStudentModal = true);
            CloseAddStudentModalCommand  = new RelayCommand(_ => ShowAddStudentModal = false);
            SaveNewStudentCommand        = new RelayCommand(async _ => await SaveNewStudentAsync());

            OpenEditStudentModalCommand  = new RelayCommand(_ => PopulateEditModal());
            CloseEditStudentModalCommand = new RelayCommand(_ => ShowEditStudentModal = false);
            SaveEditStudentCommand       = new RelayCommand(async _ => await SaveEditStudentAsync());
            UploadProfilePictureCommand  = new RelayCommand(_ => PickProfilePicture());

            PrintGoodMoralCommand        = new RelayCommand(_ => PrintGoodMoral());
            ExportTranscriptCommand      = new RelayCommand(_ => ExportTranscript());

            SelectEnrolledStudentCommand    = new RelayCommand(async p => { if (p is Student s) await SelectEnrolledStudentAsync(s); });
            ClearTranscriptSelectionCommand = new RelayCommand(_ => { HasResult = false; TranscriptResult = null; SelectedStudent = string.Empty; });
            RefreshEnrolledStudentsCommand  = new RelayCommand(async _ => await LoadEnrolledStudentsAsync());

            _ = LoadSuggestionsAsync();
            _ = LoadEnrolledStudentsAsync();
        }

        public ObservableCollection<string> StudentSuggestions { get => _studentSuggestions; set => SetProperty(ref _studentSuggestions, value); }
        public string SelectedStudent { get => _selectedStudent; set => SetProperty(ref _selectedStudent, value); }

        public StudentTranscriptResult? TranscriptResult
        {
            get => _transcriptResult;
            set
            {
                if (SetProperty(ref _transcriptResult, value))
                {
                    OnPropertyChanged(nameof(ConductScore));
                    OnPropertyChanged(nameof(StandingLabel));
                    OnPropertyChanged(nameof(StudentDisplayName));
                    OnPropertyChanged(nameof(StudentIdDisplay));
                    OnPropertyChanged(nameof(IncidentCountText));
                    OnPropertyChanged(nameof(SanctionCountText));
                    OnPropertyChanged(nameof(InterventionCountText));
                    OnPropertyChanged(nameof(SuspensionCountText));
                }
            }
        }

        public ObservableCollection<TranscriptEntryModel> FilteredEntries { get => _filteredEntries; set => SetProperty(ref _filteredEntries, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool HasResult { get => _hasResult; set => SetProperty(ref _hasResult, value); }
        public bool ShowModal { get => _showModal; set => SetProperty(ref _showModal, value); }
        public bool ShowAddStudentModal { get => _showAddStudentModal; set => SetProperty(ref _showAddStudentModal, value); }
        public bool ShowEditStudentModal { get => _showEditStudentModal; set => SetProperty(ref _showEditStudentModal, value); }

        // Enrolled Students Directory Properties
        public ObservableCollection<Student> FilteredEnrolledStudents { get => _filteredEnrolledStudents; set => SetProperty(ref _filteredEnrolledStudents, value); }
        public string EnrolledSearchText
        {
            get => _enrolledSearchText;
            set { if (SetProperty(ref _enrolledSearchText, value)) ApplyEnrolledStudentFilter(); }
        }
        public int EnrolledStudentsCount => FilteredEnrolledStudents?.Count ?? 0;

        // Summaries
        public int    ConductScore          => TranscriptResult?.ConductScore          ?? 0;
        public string StandingLabel         => TranscriptResult?.StandingLabel         ?? string.Empty;
        public string StudentDisplayName    => TranscriptResult?.StudentName           ?? string.Empty;
        public string StudentIdDisplay      => TranscriptResult?.StudentId             ?? string.Empty;
        public string IncidentCountText     => (TranscriptResult?.IncidentCount        ?? 0).ToString();
        public string SanctionCountText     => (TranscriptResult?.SanctionCount        ?? 0).ToString();
        public string InterventionCountText => (TranscriptResult?.InterventionCount    ?? 0).ToString();
        public string SuspensionCountText   => (TranscriptResult?.ActiveSuspensionCount?? 0).ToString();

        public ObservableCollection<string> CommendationCategories { get; } = new() { "Peer Leadership", "Academic Excellence", "Civic Engagement", "Artistic Achievement", "Sports Achievement", "Exemplary Conduct" };
        public ObservableCollection<string> GradeLevels { get; } = new() { "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12" };
        public ObservableCollection<string> GenderList { get; } = new() { "Male", "Female", "Other" };
        public ObservableCollection<string> TrackStrandList { get; } = new() { "Academic", "STEM", "ABM", "HUMSS", "GAS", "TVL" };

        // Merit Modal Bindings
        public string ModalCategory { get => _modalCategory; set => SetProperty(ref _modalCategory, value); }
        public int ModalPoints      { get => _modalPoints; set => SetProperty(ref _modalPoints, value); }
        public DateTime ModalDate   { get => _modalDate; set => SetProperty(ref _modalDate, value); }
        public string ModalCitation { get => _modalCitation; set => SetProperty(ref _modalCitation, value); }

        // New Student Bindings
        public string NewStudentId       { get => _newStudentId; set => SetProperty(ref _newStudentId, value); }
        public string NewFirstName       { get => _newFirstName; set => SetProperty(ref _newFirstName, value); }
        public string NewLastName        { get => _newLastName; set => SetProperty(ref _newLastName, value); }
        public string NewGradeLevel      { get => _newGradeLevel; set => SetProperty(ref _newGradeLevel, value); }
        public string NewSection         { get => _newSection; set => SetProperty(ref _newSection, value); }
        public string NewAdviser         { get => _newAdviser; set => SetProperty(ref _newAdviser, value); }
        public string NewGuardianName    { get => _newGuardianName; set => SetProperty(ref _newGuardianName, value); }
        public string NewGuardianContact { get => _newGuardianContact; set => SetProperty(ref _newGuardianContact, value); }

        // Edit Profile Bindings
        public string EditStudentId       { get => _editStudentId; set => SetProperty(ref _editStudentId, value); }
        public string EditFirstName       { get => _editFirstName; set => SetProperty(ref _editFirstName, value); }
        public string EditMiddleName      { get => _editMiddleName; set => SetProperty(ref _editMiddleName, value); }
        public string EditLastName        { get => _editLastName; set => SetProperty(ref _editLastName, value); }
        public string EditGender          { get => _editGender; set => SetProperty(ref _editGender, value); }
        public DateTime? EditDateOfBirth  { get => _editDateOfBirth; set => SetProperty(ref _editDateOfBirth, value); }
        public string EditEmail           { get => _editEmail; set => SetProperty(ref _editEmail, value); }
        public string EditPhone           { get => _editPhone; set => SetProperty(ref _editPhone, value); }
        public string EditAddress         { get => _editAddress; set => SetProperty(ref _editAddress, value); }
        public string EditGradeLevel      { get => _editGradeLevel; set => SetProperty(ref _editGradeLevel, value); }
        public string EditSection         { get => _editSection; set => SetProperty(ref _editSection, value); }
        public string EditTrackStrand     { get => _editTrackStrand; set => SetProperty(ref _editTrackStrand, value); }
        public string EditAdviser         { get => _editAdviser; set => SetProperty(ref _editAdviser, value); }
        public string EditGuardianName    { get => _editGuardianName; set => SetProperty(ref _editGuardianName, value); }
        public string EditGuardianRelation{ get => _editGuardianRelation; set => SetProperty(ref _editGuardianRelation, value); }
        public string EditGuardianContact { get => _editGuardianContact; set => SetProperty(ref _editGuardianContact, value); }
        public string EditGuardianEmail   { get => _editGuardianEmail; set => SetProperty(ref _editGuardianEmail, value); }
        public string EditMedicalNotes    { get => _editMedicalNotes; set => SetProperty(ref _editMedicalNotes, value); }
        public string EditProfilePicturePath { get => _editProfilePicturePath; set => SetProperty(ref _editProfilePicturePath, value); }

        // Commands
        public ICommand BackCommand { get; } public ICommand BackToProfilesHubCommand { get; }
        public ICommand BackToOverviewCommand { get; } public ICommand BackToHubCommand { get; }
        public ICommand LoadTranscriptCommand { get; } public ICommand OpenAddMeritCommand { get; }
        public ICommand CloseModalCommand { get; } public ICommand SaveMeritCommand { get; }
        public ICommand OpenAddStudentModalCommand { get; } public ICommand CloseAddStudentModalCommand { get; }
        public ICommand SaveNewStudentCommand { get; } public ICommand OpenEditStudentModalCommand { get; }
        public ICommand CloseEditStudentModalCommand { get; } public ICommand SaveEditStudentCommand { get; }
        public ICommand UploadProfilePictureCommand { get; } public ICommand PrintGoodMoralCommand { get; }
        public ICommand ExportTranscriptCommand { get; } public ICommand SelectEnrolledStudentCommand { get; }
        public ICommand ClearTranscriptSelectionCommand { get; } public ICommand RefreshEnrolledStudentsCommand { get; }

        public async Task LoadEnrolledStudentsAsync()
        {
            try
            {
                _allEnrolledStudents = await _service.GetEnrolledStudentsAsync();
                ApplyEnrolledStudentFilter();
            }
            catch { }
        }

        private void ApplyEnrolledStudentFilter()
        {
            if (string.IsNullOrWhiteSpace(_enrolledSearchText))
            {
                FilteredEnrolledStudents = new ObservableCollection<Student>(_allEnrolledStudents);
            }
            else
            {
                string q = _enrolledSearchText.Trim().ToLowerInvariant();
                var filtered = _allEnrolledStudents.Where(s =>
                    (s.FullName != null && s.FullName.ToLowerInvariant().Contains(q)) ||
                    (s.StudentNumber != null && s.StudentNumber.ToLowerInvariant().Contains(q)) ||
                    (s.GradeLevel != null && s.GradeLevel.ToLowerInvariant().Contains(q)) ||
                    (s.Section != null && s.Section.ToLowerInvariant().Contains(q)) ||
                    (s.Adviser != null && s.Adviser.ToLowerInvariant().Contains(q)));
                FilteredEnrolledStudents = new ObservableCollection<Student>(filtered);
            }
            OnPropertyChanged(nameof(EnrolledStudentsCount));
        }

        public async Task SelectEnrolledStudentAsync(Student student)
        {
            if (student == null) return;
            SelectedStudent = student.DisplayName;
            await LoadTranscriptAsync();
        }

        private async Task LoadSuggestionsAsync()
        {
            try
            {
                var names = await _service.GetAvailableStudentNamesAsync();
                StudentSuggestions = new ObservableCollection<string>(names);
            }
            catch { }
        }

        private async Task LoadTranscriptAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedStudent))
            {
                MessageBox.Show("Please enter or select a student name or ID to search.", "Search Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            HasResult = false;
            TranscriptResult = null;
            FilteredEntries  = new();
            try
            {
                string query = SelectedStudent.Trim();
                var result   = await _service.GetStudentTranscriptAsync(query);
                if (result == null)
                {
                    HasResult = false;
                    MessageBox.Show(
                        $"No registered student found matching \"{query}\".\n\nThis student has not been added to the system yet. Please click \"+ Add New Student\" to add their profile first.",
                        "Student Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TranscriptResult = result;
                FilteredEntries  = new ObservableCollection<TranscriptEntryModel>(result.Entries);
                HasResult        = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transcript:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private async Task SaveMeritAsync()
        {
            if (TranscriptResult == null) return;
            if (string.IsNullOrWhiteSpace(ModalCitation)) { MessageBox.Show("Please enter a citation.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try
            {
                var merit = new StudentMeritLog
                {
                    StudentIdNumber = TranscriptResult.StudentId, StudentName = TranscriptResult.StudentName,
                    CommendationType = ModalCategory, PointsAwarded = ModalPoints, DateAwarded = ModalDate,
                    AwardedBy = "admin", Citation = ModalCitation
                };
                await _service.AddMeritLogAsync(merit);
                ShowModal     = false;
                ModalCitation = string.Empty;
                ModalPoints   = 5;
                ModalDate     = DateTime.Today;
                await LoadTranscriptAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving merit:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveNewStudentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewStudentId) || string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewLastName))
            {
                MessageBox.Show("Please fill out required fields: Student ID, First Name, and Last Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newStudent = new Student
                {
                    StudentNumber = NewStudentId.Trim(), FirstName = NewFirstName.Trim(), LastName = NewLastName.Trim(),
                    GradeLevel = NewGradeLevel ?? "Grade 10", Section = NewSection?.Trim() ?? string.Empty,
                    Adviser = NewAdviser?.Trim() ?? string.Empty, GuardianName = NewGuardianName?.Trim() ?? string.Empty,
                    GuardianContact = NewGuardianContact?.Trim() ?? string.Empty
                };

                await _service.AddStudentAsync(newStudent);
                await LoadSuggestionsAsync();
                await LoadEnrolledStudentsAsync();
                SelectedStudent = $"{newStudent.FirstName} {newStudent.LastName} ({newStudent.StudentNumber})";

                ShowAddStudentModal = false;
                NewStudentId = NewFirstName = NewLastName = NewSection = NewAdviser = NewGuardianName = NewGuardianContact = string.Empty;
                await LoadTranscriptAsync();
            }
            catch (Exception ex)
            {
                string errDetail = ex.InnerException != null ? $"{ex.Message}\n\nDetails: {ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"Error saving student profile:\n{errDetail}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateEditModal()
        {
            if (TranscriptResult == null) return;
            var r = TranscriptResult;
            EditStudentId = r.StudentId; EditFirstName = r.FirstName; EditMiddleName = r.MiddleName; EditLastName = r.LastName;
            EditGender = string.IsNullOrWhiteSpace(r.Gender) ? "Male" : r.Gender; EditDateOfBirth = r.DateOfBirth ?? DateTime.Today.AddYears(-16);
            EditEmail = r.Email; EditPhone = r.Phone; EditAddress = r.Address;
            EditGradeLevel = string.IsNullOrWhiteSpace(r.GradeLevel) ? "Grade 10" : r.GradeLevel;
            EditSection = r.Section.Contains('-') ? r.Section.Split('-')[1].Trim() : r.Section;
            EditTrackStrand = string.IsNullOrWhiteSpace(r.TrackStrand) ? "Academic" : r.TrackStrand; EditAdviser = r.Adviser;
            EditGuardianName = r.GuardianName; EditGuardianRelation = string.IsNullOrWhiteSpace(r.GuardianRelation) ? "Parent" : r.GuardianRelation;
            EditGuardianContact = r.GuardianContact; EditGuardianEmail = r.GuardianEmail; EditMedicalNotes = r.MedicalNotes;
            EditProfilePicturePath = r.ProfilePicturePath;
            ShowEditStudentModal = true;
        }

        private void PickProfilePicture()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
                    Title  = "Select Student Profile Photo"
                };

                if (dialog.ShowDialog() == true)
                {
                    string avatarFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "Avatars");
                    Directory.CreateDirectory(avatarFolder);

                    string ext = Path.GetExtension(dialog.FileName);
                    string destPath = Path.Combine(avatarFolder, $"{EditStudentId}_{Guid.NewGuid():N}{ext}");
                    File.Copy(dialog.FileName, destPath, true);

                    EditProfilePicturePath = destPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading photo:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveEditStudentAsync()
        {
            if (string.IsNullOrWhiteSpace(EditStudentId) || string.IsNullOrWhiteSpace(EditFirstName) || string.IsNullOrWhiteSpace(EditLastName))
            {
                MessageBox.Show("Please fill out required fields: Student ID, First Name, and Last Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var student = new Student
                {
                    StudentNumber = EditStudentId.Trim(), FirstName = EditFirstName.Trim(), MiddleName = EditMiddleName?.Trim() ?? string.Empty,
                    LastName = EditLastName.Trim(), Gender = EditGender ?? "Male", DateOfBirth = EditDateOfBirth,
                    Email = EditEmail?.Trim() ?? string.Empty, Phone = EditPhone?.Trim() ?? string.Empty, Address = EditAddress?.Trim() ?? string.Empty,
                    GradeLevel = EditGradeLevel ?? "Grade 10", Section = EditSection?.Trim() ?? string.Empty, TrackStrand = EditTrackStrand ?? "Academic",
                    Adviser = EditAdviser?.Trim() ?? string.Empty, GuardianName = EditGuardianName?.Trim() ?? string.Empty,
                    GuardianRelation = EditGuardianRelation ?? "Parent", GuardianContact = EditGuardianContact?.Trim() ?? string.Empty,
                    GuardianEmail = EditGuardianEmail?.Trim() ?? string.Empty, MedicalNotes = EditMedicalNotes?.Trim() ?? string.Empty,
                    ProfilePicturePath = EditProfilePicturePath ?? string.Empty
                };

                await _service.UpdateStudentAsync(student);
                ShowEditStudentModal = false;
                await LoadSuggestionsAsync();
                await LoadEnrolledStudentsAsync();
                SelectedStudent = $"{student.FirstName} {student.LastName} ({student.StudentNumber})";
                await LoadTranscriptAsync();
            }
            catch (Exception ex)
            {
                string errDetail = ex.InnerException != null ? $"{ex.Message}\n\nDetails: {ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"Error updating student profile:\n{errDetail}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintGoodMoral()
        {
            if (TranscriptResult == null)
            {
                MessageBox.Show("Please select and load a student transcript first.", "No Student Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TranscriptResult.IsEligible || TranscriptResult.ConductScore < 75 || TranscriptResult.ActiveSuspensionCount > 0)
            {
                MessageBox.Show($"Student '{TranscriptResult.StudentName}' is currently INELIGIBLE for a Certificate of Good Moral Character.\n\n" +
                                $"Current Conduct Score: {TranscriptResult.ConductScore}/100 (Minimum required: 75/100)\n" +
                                $"Active Suspensions: {TranscriptResult.ActiveSuspensionCount} active order(s)\n\n" +
                                "All active suspensions must be cleared and conduct score restored to 75+ before issuance.",
                                "Certificate Ineligible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sanitizedName = string.Concat(TranscriptResult.StudentName.Split(Path.GetInvalidFileNameChars()));
                var dialog = new SaveFileDialog
                {
                    FileName = $"{sanitizedName}_GoodMoral.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Good Moral Certificate"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateGoodMoralCertificate(dialog.FileName, TranscriptResult);
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating Good Moral Certificate:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportTranscript()
        {
            if (TranscriptResult == null)
            {
                MessageBox.Show("Please select and load a student transcript first.", "No Student Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sanitizedName = string.Concat(TranscriptResult.StudentName.Split(Path.GetInvalidFileNameChars()));
                var dialog = new SaveFileDialog
                {
                    FileName = $"{sanitizedName}_ConductTranscript.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Conduct Transcript PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateConductTranscript(dialog.FileName, TranscriptResult);
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting Conduct Transcript PDF:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
