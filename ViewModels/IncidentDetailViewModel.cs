using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class IncidentDetailViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly IRuleEngineService _ruleEngineService;
        private readonly Action _onClose;
        private Incident _incident;
        private bool _isEditMode;
        private string _errorMessage = string.Empty;

        // Rule-Based Analysis live properties
        private string _liveCategory = "General Misconduct";
        private IncidentSeverity _liveSeverity = IncidentSeverity.Low;
        private RiskLevel _liveRisk = RiskLevel.Low;
        private string _liveRecommendedAction = string.Empty;
        private bool _isRepeatAlertVisible;
        private string _repeatAlertText = string.Empty;
        private ObservableCollection<string> _triggeredRules = new();

        public IncidentDetailViewModel(IIncidentService incidentService, IRuleEngineService ruleEngineService, Incident? incident, Action onClose)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _ruleEngineService = ruleEngineService ?? throw new ArgumentNullException(nameof(ruleEngineService));
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));

            Categories = new ObservableCollection<string>(_ruleEngineService.GetStandardCategories());

            if (incident != null)
            {
                _isEditMode = true;
                _incident = new Incident
                {
                    Id = incident.Id,
                    SyncId = incident.SyncId,
                    IncidentNumber = incident.IncidentNumber,
                    Title = incident.Title,
                    Category = incident.Category,
                    Description = incident.Description,
                    Location = incident.Location,
                    IncidentDate = incident.IncidentDate,
                    PrimaryInvolvedStudent = !string.IsNullOrWhiteSpace(incident.PrimaryInvolvedStudent)
                        ? incident.PrimaryInvolvedStudent
                        : (incident.Student?.FullName ?? string.Empty),
                    ReportingStaffTeacher = !string.IsNullOrWhiteSpace(incident.ReportingStaffTeacher)
                        ? incident.ReportingStaffTeacher
                        : (incident.Reporter?.FullName ?? string.Empty),
                    InvolvedPersons = incident.InvolvedPersons,
                    Witnesses = incident.Witnesses,
                    Status = incident.Status,
                    Severity = incident.Severity,
                    Risk = incident.Risk,
                    RecommendedAction = incident.RecommendedAction,
                    IsRepeatIncident = incident.IsRepeatIncident,
                    RepeatIncidentCount = incident.RepeatIncidentCount,
                    ActionTaken = incident.ActionTaken,
                    Notes = incident.Notes,
                    CreatedAt = incident.CreatedAt,
                    UpdatedAt = incident.UpdatedAt,
                    IsSynced = incident.IsSynced
                };
            }
            else
            {
                _isEditMode = false;
                _incident = new Incident
                {
                    IncidentDate = DateTime.Now,
                    Status = IncidentStatus.Reported,
                    Severity = IncidentSeverity.Low,
                    Risk = RiskLevel.Low,
                    Category = "General Misconduct",
                    CreatedBy = !string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : string.Empty,
                    ReportingStaffTeacher = !string.IsNullOrWhiteSpace(SessionManager.FullName)
                        ? SessionManager.FullName
                        : (!string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : string.Empty)
                };

                _ = InitializeNewIncidentNumber();
            }

            SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(() => _onClose());
            ApplyRuleRecommendationsCommand = new RelayCommand(ApplyRecommendations);
            SelectStudentSuggestionCommand = new RelayCommand(param => SelectStudentSuggestion(param));

            _ = LoadEnrolledStudentsAsync();
            TriggerLiveAnalysis();
        }

        public bool IsEditMode => _isEditMode;
        public string TitleText => _isEditMode ? $"Edit Incident {_incident.IncidentNumber}" : "Report New Incident (Rule-Assisted)";

        public ObservableCollection<string> Categories { get; }

        public string IncidentNumber
        {
            get => _incident.IncidentNumber;
            set { _incident.IncidentNumber = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _incident.Title;
            set { _incident.Title = value; OnPropertyChanged(); TriggerLiveAnalysis(); }
        }

        public string Category
        {
            get => _incident.Category;
            set { _incident.Category = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _incident.Description;
            set { _incident.Description = value; OnPropertyChanged(); TriggerLiveAnalysis(); }
        }

        public string Location
        {
            get => _incident.Location;
            set { _incident.Location = value; OnPropertyChanged(); TriggerLiveAnalysis(); }
        }

        public DateTime IncidentDate
        {
            get => _incident.IncidentDate;
            set { _incident.IncidentDate = value; OnPropertyChanged(); }
        }

        // Primary Involved Student with Live Enrolled Student Suggestions
        private List<string> _allEnrolledStudents = new();
        private ObservableCollection<string> _studentSuggestions = new();
        private bool _isStudentSuggestionsOpen;

        public ObservableCollection<string> StudentSuggestions
        {
            get => _studentSuggestions;
            set { _studentSuggestions = value; OnPropertyChanged(); }
        }

        public bool IsStudentSuggestionsOpen
        {
            get => _isStudentSuggestionsOpen;
            set { _isStudentSuggestionsOpen = value; OnPropertyChanged(); }
        }

        public ICommand SelectStudentSuggestionCommand { get; }

        public string PrimaryInvolvedStudent
        {
            get => _incident.PrimaryInvolvedStudent;
            set
            {
                if (_incident.PrimaryInvolvedStudent != value)
                {
                    _incident.PrimaryInvolvedStudent = value;
                    OnPropertyChanged();
                    TriggerLiveAnalysis();
                    UpdateStudentSuggestions(value);
                }
            }
        }

        private async Task LoadEnrolledStudentsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                _allEnrolledStudents = await db.Students
                    .AsNoTracking()
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => $"{s.FirstName} {s.LastName} ({s.StudentNumber})")
                    .ToListAsync();
            }
            catch { }
        }

        private void UpdateStudentSuggestions(string? input)
        {
            if (string.IsNullOrWhiteSpace(input) || _allEnrolledStudents.Count == 0)
            {
                StudentSuggestions.Clear();
                IsStudentSuggestionsOpen = false;
                return;
            }

            string filter = input.Trim().ToLowerInvariant();
            var matches = _allEnrolledStudents
                .Where(s => s.ToLowerInvariant().Contains(filter))
                .Take(8)
                .ToList();

            StudentSuggestions.Clear();
            foreach (var m in matches)
            {
                StudentSuggestions.Add(m);
            }

            // Show popup when matches exist and the input is not yet identical to the chosen student
            IsStudentSuggestionsOpen = matches.Count > 0 && !matches.Any(m => m.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void SelectStudentSuggestion(object? param)
        {
            if (param is string studentName && !string.IsNullOrWhiteSpace(studentName))
            {
                _incident.PrimaryInvolvedStudent = studentName;
                OnPropertyChanged(nameof(PrimaryInvolvedStudent));
                TriggerLiveAnalysis();
                StudentSuggestions.Clear();
                IsStudentSuggestionsOpen = false;
            }
        }

        public string ReportingStaffTeacher
        {
            get => _incident.ReportingStaffTeacher;
            set { _incident.ReportingStaffTeacher = value; OnPropertyChanged(); }
        }

        public string InvolvedPersons
        {
            get => _incident.InvolvedPersons;
            set { _incident.InvolvedPersons = value; OnPropertyChanged(); }
        }

        public string Witnesses
        {
            get => _incident.Witnesses;
            set { _incident.Witnesses = value; OnPropertyChanged(); }
        }

        public IncidentStatus Status
        {
            get => _incident.Status;
            set { _incident.Status = value; OnPropertyChanged(); }
        }

        public IncidentSeverity Severity
        {
            get => _incident.Severity;
            set { _incident.Severity = value; OnPropertyChanged(); }
        }

        public RiskLevel Risk
        {
            get => _incident.Risk;
            set { _incident.Risk = value; OnPropertyChanged(); }
        }

        public string ActionTaken
        {
            get => _incident.ActionTaken;
            set { _incident.ActionTaken = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _incident.Notes;
            set { _incident.Notes = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Live Rule Engine Properties
        public string LiveCategory
        {
            get => _liveCategory;
            set => SetProperty(ref _liveCategory, value);
        }

        public IncidentSeverity LiveSeverity
        {
            get => _liveSeverity;
            set => SetProperty(ref _liveSeverity, value);
        }

        public RiskLevel LiveRisk
        {
            get => _liveRisk;
            set => SetProperty(ref _liveRisk, value);
        }

        public string LiveRecommendedAction
        {
            get => _liveRecommendedAction;
            set => SetProperty(ref _liveRecommendedAction, value);
        }

        public bool IsRepeatAlertVisible
        {
            get => _isRepeatAlertVisible;
            set => SetProperty(ref _isRepeatAlertVisible, value);
        }

        public string RepeatAlertText
        {
            get => _repeatAlertText;
            set => SetProperty(ref _repeatAlertText, value);
        }

        public ObservableCollection<string> TriggeredRules => _triggeredRules;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApplyRuleRecommendationsCommand { get; }

        private async Task InitializeNewIncidentNumber()
        {
            IncidentNumber = await _ruleEngineService.GenerateIncidentNumberAsync();
            OnPropertyChanged(nameof(TitleText));
        }

        private async void TriggerLiveAnalysis()
        {
            string narrative = $"{Title} {Description}";
            var result = await _ruleEngineService.AnalyzeAsync(narrative, Location, PrimaryInvolvedStudent, Category);

            LiveCategory = result.SuggestedCategory;
            LiveSeverity = result.SuggestedSeverity;
            LiveRisk = result.SuggestedRisk;
            LiveRecommendedAction = result.RecommendedAction;
            IsRepeatAlertVisible = result.IsRepeatOffender;
            RepeatAlertText = result.RepeatAlertMessage;

            TriggeredRules.Clear();
            foreach (var r in result.TriggeredRules)
            {
                TriggeredRules.Add(r);
            }
        }

        private void ApplyRecommendations()
        {
            Category = LiveCategory;
            Severity = LiveSeverity;
            Risk = LiveRisk;
            ActionTaken = string.IsNullOrEmpty(ActionTaken) ? LiveRecommendedAction : $"{ActionTaken}\n\n[Rule Engine Recommendation]: {LiveRecommendedAction}";
        }

        private bool CanSave() => true;

        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Please enter the incident title.";
                return;
            }

            if (string.IsNullOrWhiteSpace(PrimaryInvolvedStudent))
            {
                ErrorMessage = "Please enter the primary involved student's name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ReportingStaffTeacher))
            {
                ReportingStaffTeacher = !string.IsNullOrWhiteSpace(SessionManager.FullName)
                    ? SessionManager.FullName
                    : (!string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : string.Empty);
            }

            if (string.IsNullOrWhiteSpace(ReportingStaffTeacher))
            {
                ErrorMessage = "Please enter the reporting staff or teacher's name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ErrorMessage = "Please enter the incident description.";
                return;
            }

            try
            {
                _incident.Title = _incident.Title.Trim();
                _incident.PrimaryInvolvedStudent = PrimaryInvolvedStudent.Trim();
                _incident.ReportingStaffTeacher = ReportingStaffTeacher.Trim();
                if (string.IsNullOrWhiteSpace(_incident.CreatedBy))
                {
                    _incident.CreatedBy = !string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : string.Empty;
                }
                _incident.Location = (_incident.Location ?? string.Empty).Trim();
                _incident.Description = (_incident.Description ?? string.Empty).Trim();
                _incident.RecommendedAction = LiveRecommendedAction;
                _incident.IsRepeatIncident = IsRepeatAlertVisible;

                if (_isEditMode)
                {
                    await _incidentService.UpdateIncidentAsync(_incident);
                }
                else
                {
                    await _incidentService.AddIncidentAsync(_incident);
                }

                // Log Administrative Notification Event for System Admins & Discipline Officers
                try
                {
                    var logService = new SecurityAccessLogService(new AppDbContext());
                    await logService.LogAccessEventAsync(
                        username: !string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : (!string.IsNullOrWhiteSpace(SessionManager.Role) ? SessionManager.Role : "Reporter"),
                        eventType: "Admin Incident Notification",
                        status: "Notice Dispatched",
                        details: $"[ADMIN NOTIFICATION]: New incident '{_incident.IncidentNumber}' ('{_incident.Title}') filed by {ReportingStaffTeacher} for student '{PrimaryInvolvedStudent}'."
                    );
                }
                catch { }

                System.Windows.MessageBox.Show(
                    $"Incident Report '{_incident.IncidentNumber}' Submitted Successfully!\n\nAn automated notification has been logged for System Administrators & Discipline Officers to review.",
                    "Incident Filed & Admin Notified",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                _onClose();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving incident: {ex.Message}";
            }
        }
    }
}