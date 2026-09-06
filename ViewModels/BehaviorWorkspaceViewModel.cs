using SmartSchoolIncidentManagementSystem.Data;
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
    public partial class BehaviorWorkspaceViewModel : ObservableObject
    {
        private readonly IBehavioralTranscriptService _transcriptService;
        private readonly IAnecdotalService _anecdotalService;
        private readonly IRestorativeService _restorativeService;
        private readonly IActionPlanService _planService;
        private readonly IPdfExportService _pdfService;
        private readonly IStudentService _studentService;

        private List<Student> _allStudents = new();
        private ObservableCollection<Student> _studentSuggestions = new();
        private Student? _selectedStudent;
        private string _studentSearchText = string.Empty;
        private bool _isStudentSuggestionsOpen;
        private bool _isLoading;

        // Current Student Stats
        private int _conductScore = 100;
        private string _standingLabel = "Exemplary Moral Standing";
        private string _standingColor = "#10B981";
        private int _totalIncidentsCount;
        private int _totalMeritsCount;
        private int _activeRestorativePlansCount;
        private int _activeIapCount;

        // Tab Data Collections
        private ObservableCollection<TranscriptEntryModel> _timelineEntries = new();
        private ObservableCollection<AnecdotalRecordItem> _anecdotalRecords = new();
        private ObservableCollection<RestorativePlan> _restorativePlans = new();
        private ObservableCollection<IndividualActionPlan> _actionPlans = new();

        // --- Modals State ---
        // 1. Add Merit Modal
        private bool _isAddMeritModalOpen;
        private string _meritType = "Peer Leadership";
        private int _meritPoints = 5;
        private string _meritCitation = string.Empty;

        // 2. Add Anecdotal Note Modal
        private bool _isAddAnecdotalModalOpen;
        private string _anecdotalSubject = "Classroom Observation";
        private string _anecdotalType = "General Behavior";
        private string _anecdotalNarrative = string.Empty;
        private string _anecdotalActionTaken = string.Empty;

        // 3. Add Restorative Plan Modal
        private bool _isAddRestorativeModalOpen;
        private string _restorativeType = "Community Service";
        private int _restorativeTargetMetric = 10;
        private string _restorativeObjectives = string.Empty;

        public BehaviorWorkspaceViewModel(
            IBehavioralTranscriptService? transcriptService = null,
            IAnecdotalService? anecdotalService = null,
            IRestorativeService? restorativeService = null,
            IActionPlanService? planService = null,
            IPdfExportService? pdfService = null,
            IStudentService? studentService = null)
        {
            var db = new AppDbContext();
            _transcriptService = transcriptService ?? new BehavioralTranscriptService(db);
            _anecdotalService = anecdotalService ?? new AnecdotalService();
            _restorativeService = restorativeService ?? new RestorativeService(db);
            _planService = planService ?? new ActionPlanService(db);
            _pdfService = pdfService ?? new PdfExportService();
            _studentService = studentService ?? new StudentService(db);

            MeritTypeOptions = new ObservableCollection<string>
            {
                "Peer Leadership", "Academic Excellence", "Civic / Campus Service", "Good Samaritan Act", "Sportsmanship"
            };

            AnecdotalTypeOptions = new ObservableCollection<string>
            {
                "Positive Conduct", "Minor Infraction", "Academic Focus", "Attendance", "Interpersonal Conflict"
            };

            RestorativeTypeOptions = new ObservableCollection<string>
            {
                "Community Service", "Individual Counseling", "Peer Mediation", "Behavioral Contract", "Anger Management Workshop"
            };

            // Commands
            SelectStudentCommand = new RelayCommand(param =>
            {
                if (param is Student s) SelectStudent(s);
            });

            OpenAddMeritCommand = new RelayCommand(() =>
            {
                if (SelectedStudent == null) return;
                MeritCitation = string.Empty;
                MeritPoints = 5;
                IsAddMeritModalOpen = true;
            }, () => SelectedStudent != null && CanManageBehavior);

            CloseAddMeritCommand = new RelayCommand(() => IsAddMeritModalOpen = false);
            SaveMeritCommand = new RelayCommand(async () => await SaveMeritAsync());

            OpenAddAnecdotalCommand = new RelayCommand(() =>
            {
                if (SelectedStudent == null) return;
                AnecdotalNarrative = string.Empty;
                AnecdotalActionTaken = string.Empty;
                IsAddAnecdotalModalOpen = true;
            }, () => SelectedStudent != null);

            CloseAddAnecdotalCommand = new RelayCommand(() => IsAddAnecdotalModalOpen = false);
            SaveAnecdotalCommand = new RelayCommand(async () => await SaveAnecdotalAsync());

            OpenAddRestorativeCommand = new RelayCommand(() =>
            {
                if (SelectedStudent == null) return;
                RestorativeObjectives = string.Empty;
                RestorativeTargetMetric = 10;
                IsAddRestorativeModalOpen = true;
            }, () => SelectedStudent != null && CanManageBehavior);

            CloseAddRestorativeCommand = new RelayCommand(() => IsAddRestorativeModalOpen = false);
            SaveRestorativeCommand = new RelayCommand(async () => await SaveRestorativeAsync());

            ExportTranscriptCommand = new RelayCommand(async () => await ExportTranscriptAsync(), () => SelectedStudent != null);
            PrintGoodMoralCommand = new RelayCommand(async () => await PrintGoodMoralAsync(), () => SelectedStudent != null);

            _ = InitializeStudentsAsync();
        }

        public ObservableCollection<Student> StudentSuggestions => _studentSuggestions;
        public ObservableCollection<string> MeritTypeOptions { get; }
        public ObservableCollection<string> AnecdotalTypeOptions { get; }
        public ObservableCollection<string> RestorativeTypeOptions { get; }

        public ObservableCollection<TranscriptEntryModel> TimelineEntries => _timelineEntries;
        public ObservableCollection<AnecdotalRecordItem> AnecdotalRecords => _anecdotalRecords;
        public ObservableCollection<RestorativePlan> RestorativePlans => _restorativePlans;
        public ObservableCollection<IndividualActionPlan> ActionPlans => _actionPlans;

        public bool CanManageBehavior => SessionManager.HasPermission("System Administrator,Discipline Officer,Guidance Counselor");

        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set => SetProperty(ref _selectedStudent, value);
        }

        public string StudentSearchText
        {
            get => _studentSearchText;
            set
            {
                if (SetProperty(ref _studentSearchText, value))
                {
                    FilterStudentSuggestions(value);
                }
            }
        }

        public bool IsStudentSuggestionsOpen
        {
            get => _isStudentSuggestionsOpen;
            set => SetProperty(ref _isStudentSuggestionsOpen, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int ConductScore { get => _conductScore; set => SetProperty(ref _conductScore, value); }
        public string StandingLabel { get => _standingLabel; set => SetProperty(ref _standingLabel, value); }
        public string StandingColor { get => _standingColor; set => SetProperty(ref _standingColor, value); }
        public int TotalIncidentsCount { get => _totalIncidentsCount; set => SetProperty(ref _totalIncidentsCount, value); }
        public int TotalMeritsCount { get => _totalMeritsCount; set => SetProperty(ref _totalMeritsCount, value); }
        public int ActiveRestorativePlansCount { get => _activeRestorativePlansCount; set => SetProperty(ref _activeRestorativePlansCount, value); }
        public int ActiveIapCount { get => _activeIapCount; set => SetProperty(ref _activeIapCount, value); }

        // Modal properties
        public bool IsAddMeritModalOpen { get => _isAddMeritModalOpen; set => SetProperty(ref _isAddMeritModalOpen, value); }
        public string MeritType { get => _meritType; set => SetProperty(ref _meritType, value); }
        public int MeritPoints { get => _meritPoints; set => SetProperty(ref _meritPoints, value); }
        public string MeritCitation { get => _meritCitation; set => SetProperty(ref _meritCitation, value); }

        public bool IsAddAnecdotalModalOpen { get => _isAddAnecdotalModalOpen; set => SetProperty(ref _isAddAnecdotalModalOpen, value); }
        public string AnecdotalSubject { get => _anecdotalSubject; set => SetProperty(ref _anecdotalSubject, value); }
        public string AnecdotalType { get => _anecdotalType; set => SetProperty(ref _anecdotalType, value); }
        public string AnecdotalNarrative { get => _anecdotalNarrative; set => SetProperty(ref _anecdotalNarrative, value); }
        public string AnecdotalActionTaken { get => _anecdotalActionTaken; set => SetProperty(ref _anecdotalActionTaken, value); }

        public bool IsAddRestorativeModalOpen { get => _isAddRestorativeModalOpen; set => SetProperty(ref _isAddRestorativeModalOpen, value); }
        public string RestorativeType { get => _restorativeType; set => SetProperty(ref _restorativeType, value); }
        public int RestorativeTargetMetric { get => _restorativeTargetMetric; set => SetProperty(ref _restorativeTargetMetric, value); }
        public string RestorativeObjectives { get => _restorativeObjectives; set => SetProperty(ref _restorativeObjectives, value); }

        // Commands
        public ICommand SelectStudentCommand { get; }
        public ICommand OpenAddMeritCommand { get; }
        public ICommand CloseAddMeritCommand { get; }
        public ICommand SaveMeritCommand { get; }
        public ICommand OpenAddAnecdotalCommand { get; }
        public ICommand CloseAddAnecdotalCommand { get; }
        public ICommand SaveAnecdotalCommand { get; }
        public ICommand OpenAddRestorativeCommand { get; }
        public ICommand CloseAddRestorativeCommand { get; }
        public ICommand SaveRestorativeCommand { get; }
        public ICommand ExportTranscriptCommand { get; }
        public ICommand PrintGoodMoralCommand { get; }

        private async Task InitializeStudentsAsync()
        {
            try
            {
                _allStudents = await _studentService.GetAllStudentsAsync();
                if (_allStudents.Count > 0)
                {
                    SelectStudent(_allStudents[0]);
                }
            }
            catch { }
        }

        private void FilterStudentSuggestions(string? input)
        {
            if (string.IsNullOrWhiteSpace(input) || _allStudents.Count == 0)
            {
                StudentSuggestions.Clear();
                IsStudentSuggestionsOpen = false;
                return;
            }

            var matches = _allStudents
                .Where(s => s.FullName.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                            s.StudentNumber.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .ToList();

            StudentSuggestions.Clear();
            foreach (var s in matches) StudentSuggestions.Add(s);

            IsStudentSuggestionsOpen = matches.Count > 0 && !matches.Any(m => m.FullName.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public async void SelectStudent(Student s)
        {
            SelectedStudent = s;
            StudentSearchText = s.FullName;
            StudentSuggestions.Clear();
            IsStudentSuggestionsOpen = false;

            await LoadStudentBehaviorDataAsync(s);
        }

        private async Task LoadStudentBehaviorDataAsync(Student s)
        {
            IsLoading = true;
            try
            {
                // 1. Load Transcript & Conduct Score
                var transcript = await _transcriptService.GetStudentTranscriptAsync(s.StudentNumber);
                if (transcript != null)
                {
                    ConductScore = transcript.ConductScore;
                    StandingLabel = transcript.StandingLabel;
                    StandingColor = ConductScore >= 85 ? "#10B981" : (ConductScore >= 70 ? "#F59E0B" : "#EF4444");
                    TotalIncidentsCount = transcript.IncidentCount;
                    TotalMeritsCount = transcript.Entries.Count(e => e.ScoreImpact > 0);

                    _timelineEntries.Clear();
                    foreach (var entry in transcript.Entries) _timelineEntries.Add(entry);
                }
                else
                {
                    ConductScore = 100;
                    StandingLabel = "GOOD STANDING";
                    StandingColor = "#10B981";
                    TotalIncidentsCount = 0;
                    TotalMeritsCount = 0;
                    _timelineEntries.Clear();
                }

                // 2. Load Anecdotal Records
                var anecdotes = await _anecdotalService.GetAllAsync(s.StudentNumber);
                if (anecdotes.Count == 0)
                {
                    anecdotes = await _anecdotalService.GetAllAsync(s.FullName);
                }
                _anecdotalRecords.Clear();
                foreach (var a in anecdotes) _anecdotalRecords.Add(a);

                // 3. Load Restorative Plans
                var plans = await _restorativeService.GetAllInterventionPlansAsync();
                var studentPlans = plans.Where(p => p.StudentIdNumber == s.StudentNumber || p.StudentName.Equals(s.FullName, StringComparison.OrdinalIgnoreCase)).ToList();
                _restorativePlans.Clear();
                foreach (var p in studentPlans) _restorativePlans.Add(p);
                ActiveRestorativePlansCount = studentPlans.Count(p => p.Status == "Active");

                // 4. Load Action Plans (IAPs)
                var iaps = await _planService.GetAllPlansAsync();
                var studentIaps = iaps.Where(p => p.StudentId == s.StudentNumber || p.StudentName.Equals(s.FullName, StringComparison.OrdinalIgnoreCase)).ToList();
                _actionPlans.Clear();
                foreach (var iap in studentIaps) _actionPlans.Add(iap);
                ActiveIapCount = studentIaps.Count;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveMeritAsync()
        {
            if (SelectedStudent == null || string.IsNullOrWhiteSpace(MeritCitation)) return;

            try
            {
                var merit = new StudentMeritLog
                {
                    StudentIdNumber = SelectedStudent.StudentNumber,
                    StudentName = SelectedStudent.FullName,
                    CommendationType = MeritType,
                    PointsAwarded = MeritPoints,
                    Citation = MeritCitation.Trim(),
                    AwardedBy = SessionManager.FullName,
                    DateAwarded = DateTime.Now
                };

                await _transcriptService.AddMeritLogAsync(merit);

                IsAddMeritModalOpen = false;
                await LoadStudentBehaviorDataAsync(SelectedStudent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save merit: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveAnecdotalAsync()
        {
            if (SelectedStudent == null || string.IsNullOrWhiteSpace(AnecdotalNarrative)) return;

            try
            {
                var item = new AnecdotalRecordItem
                {
                    StudentIdNumber = SelectedStudent.StudentNumber,
                    StudentName = SelectedStudent.FullName,
                    GradeSection = $"{SelectedStudent.GradeLevel} - {SelectedStudent.Section}",
                    TeacherName = SessionManager.FullName,
                    ObservationDate = DateTime.Now,
                    SettingOrSubject = AnecdotalSubject.Trim(),
                    ObservationType = AnecdotalType,
                    NarrativeObservation = AnecdotalNarrative.Trim(),
                    ActionTakenByTeacher = AnecdotalActionTaken.Trim(),
                    Status = "Logged"
                };

                await _anecdotalService.AddAsync(item);
                IsAddAnecdotalModalOpen = false;
                await LoadStudentBehaviorDataAsync(SelectedStudent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save anecdotal observation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveRestorativeAsync()
        {
            if (SelectedStudent == null) return;

            try
            {
                var plan = new RestorativePlan
                {
                    PlanId = await _restorativeService.GeneratePlanIdAsync(),
                    StudentIdNumber = SelectedStudent.StudentNumber,
                    StudentName = SelectedStudent.FullName,
                    PlanType = RestorativeType,
                    AssignedStaff = SessionManager.FullName,
                    TargetMetric = RestorativeTargetMetric,
                    CompletedMetric = 0,
                    StartDate = DateTime.Today,
                    DeadlineDate = DateTime.Today.AddDays(14),
                    Objectives = RestorativeObjectives.Trim(),
                    Status = "Active"
                };

                await _restorativeService.AddInterventionPlanAsync(plan);
                IsAddRestorativeModalOpen = false;
                await LoadStudentBehaviorDataAsync(SelectedStudent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save restorative plan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportTranscriptAsync()
        {
            if (SelectedStudent == null) return;
            try
            {
                var transcript = await _transcriptService.GetStudentTranscriptAsync(SelectedStudent.StudentNumber);
                if (transcript == null)
                {
                    MessageBox.Show("Unable to generate transcript: Student record not found.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Transcript_{SelectedStudent.StudentNumber}.pdf");
                _pdfService.GenerateConductTranscript(path, transcript);
                MessageBox.Show($"Behavioral Transcript exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF Export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PrintGoodMoralAsync()
        {
            if (SelectedStudent == null) return;
            try
            {
                var transcript = await _transcriptService.GetStudentTranscriptAsync(SelectedStudent.StudentNumber);
                if (transcript == null)
                {
                    MessageBox.Show("Unable to generate Good Moral certificate: Student record not found.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"GoodMoral_{SelectedStudent.StudentNumber}.pdf");
                _pdfService.GenerateGoodMoralCertificate(path, transcript);
                MessageBox.Show($"Certificate of Good Moral Character exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Certificate Export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
