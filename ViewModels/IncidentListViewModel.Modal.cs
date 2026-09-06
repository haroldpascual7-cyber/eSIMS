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
    public partial class IncidentListViewModel
    {
        // --- Incident Modal (Create / View / Edit) State ---
        private bool _isIncidentModalOpen;
        private bool _isModalEditing;
        private bool _isModalNewRecord;
        private string _modalTitle = "Incident Record";
        private string _modalErrorMessage = string.Empty;
        private int _modalIncidentPk;

        private string _modalIncidentNumber = string.Empty;
        private string _modalTitleField = string.Empty;
        private string _modalCategory = "General Misconduct";
        private string _modalDescription = string.Empty;
        private string _modalLocation = "Campus Grounds";
        private DateTime _modalDate = DateTime.Now;
        private string _modalTime = DateTime.Now.ToString("HH:mm");
        private string _modalStudentName = string.Empty;
        private string _modalStudentNumber = string.Empty;
        private string _modalGradeSection = string.Empty;
        private string _modalParentName = string.Empty;
        private string _modalParentEmail = string.Empty;
        private string _modalReporter = string.Empty;
        private IncidentStatus _modalStatus = IncidentStatus.Reported;
        private IncidentSeverity _modalSeverity = IncidentSeverity.Low;
        private RiskLevel _modalRisk = RiskLevel.Low;
        private string _modalActionTaken = string.Empty;
        private string _modalNotes = string.Empty;

        // Automated Parent Email Notification
        private bool _sendParentNotification = true;
        private string _notificationStatusText = "Pending Save";

        // Live Rule Engine Analysis Results
        private string _liveSuggestedCategory = "General Misconduct";
        private IncidentSeverity _liveSuggestedSeverity = IncidentSeverity.Low;
        private RiskLevel _liveSuggestedRisk = RiskLevel.Low;
        private string _liveRecommendedAction = string.Empty;
        private bool _isRepeatOffenderAlert;
        private string _repeatAlertMessage = string.Empty;
        private ObservableCollection<string> _triggeredRules = new();

        // Student Auto-Suggestions
        private List<Student> _allEnrolledStudents = new();
        private ObservableCollection<Student> _studentSuggestions = new();
        private bool _isStudentSuggestionsOpen;

        public ObservableCollection<string> TriggeredRules => _triggeredRules;
        public ObservableCollection<Student> StudentSuggestions => _studentSuggestions;

        // --- Modal Properties ---
        public bool IsIncidentModalOpen { get => _isIncidentModalOpen; set => SetProperty(ref _isIncidentModalOpen, value); }
        public bool IsModalEditing 
        { 
            get => _isModalEditing; 
            set 
            { 
                if (SetProperty(ref _isModalEditing, value))
                {
                    OnPropertyChanged(nameof(IsModalReadOnly));
                    OnPropertyChanged(nameof(CanCancelEdit));
                }
            } 
        }
        public bool IsModalReadOnly => !IsModalEditing;
        public bool IsModalNewRecord 
        { 
            get => _isModalNewRecord; 
            set 
            { 
                if (SetProperty(ref _isModalNewRecord, value))
                {
                    OnPropertyChanged(nameof(CanDeleteModalIncident));
                    OnPropertyChanged(nameof(CanInvestigateModalIncident));
                    OnPropertyChanged(nameof(CanCancelEdit));
                    OnPropertyChanged(nameof(SaveModalButtonText));
                }
            } 
        }
        public bool CanDeleteModalIncident => !IsModalNewRecord && CanDeleteIncident;
        public bool CanInvestigateModalIncident => !IsModalNewRecord && CanOpenInvestigation;
        public bool CanCancelEdit => IsModalEditing && !IsModalNewRecord;
        public string SaveModalButtonText => IsModalNewRecord ? "Save Incident" : "Update & Save";
        public string ModalTitle { get => _modalTitle; set => SetProperty(ref _modalTitle, value); }
        public string ModalErrorMessage { get => _modalErrorMessage; set => SetProperty(ref _modalErrorMessage, value); }

        public string ModalIncidentNumber { get => _modalIncidentNumber; set => SetProperty(ref _modalIncidentNumber, value); }
        public string ModalTitleField { get => _modalTitleField; set { if (SetProperty(ref _modalTitleField, value)) TriggerLiveAnalysis(); } }
        public string ModalCategory { get => _modalCategory; set { if (SetProperty(ref _modalCategory, value)) TriggerLiveAnalysis(); } }
        public string ModalDescription { get => _modalDescription; set { if (SetProperty(ref _modalDescription, value)) TriggerLiveAnalysis(); } }
        public string ModalLocation { get => _modalLocation; set { if (SetProperty(ref _modalLocation, value)) TriggerLiveAnalysis(); } }
        public DateTime ModalDate { get => _modalDate; set => SetProperty(ref _modalDate, value); }
        public string ModalTime { get => _modalTime; set => SetProperty(ref _modalTime, value); }

        public string ModalStudentName
        {
            get => _modalStudentName;
            set
            {
                if (SetProperty(ref _modalStudentName, value))
                {
                    UpdateStudentSuggestions(value);
                    TriggerLiveAnalysis();
                }
            }
        }

        public string ModalStudentNumber { get => _modalStudentNumber; set => SetProperty(ref _modalStudentNumber, value); }
        public string ModalGradeSection { get => _modalGradeSection; set => SetProperty(ref _modalGradeSection, value); }
        public string ModalParentName { get => _modalParentName; set => SetProperty(ref _modalParentName, value); }
        public string ModalParentEmail { get => _modalParentEmail; set => SetProperty(ref _modalParentEmail, value); }
        public string ModalReporter { get => _modalReporter; set => SetProperty(ref _modalReporter, value); }
        public IncidentStatus ModalStatus { get => _modalStatus; set => SetProperty(ref _modalStatus, value); }
        public IncidentSeverity ModalSeverity { get => _modalSeverity; set => SetProperty(ref _modalSeverity, value); }
        public RiskLevel ModalRisk { get => _modalRisk; set => SetProperty(ref _modalRisk, value); }
        public string ModalActionTaken { get => _modalActionTaken; set => SetProperty(ref _modalActionTaken, value); }
        public string ModalNotes { get => _modalNotes; set => SetProperty(ref _modalNotes, value); }

        public bool SendParentNotification { get => _sendParentNotification; set => SetProperty(ref _sendParentNotification, value); }
        public string NotificationStatusText { get => _notificationStatusText; set => SetProperty(ref _notificationStatusText, value); }

        public string LiveSuggestedCategory { get => _liveSuggestedCategory; set => SetProperty(ref _liveSuggestedCategory, value); }
        public IncidentSeverity LiveSuggestedSeverity { get => _liveSuggestedSeverity; set => SetProperty(ref _liveSuggestedSeverity, value); }
        public RiskLevel LiveSuggestedRisk { get => _liveSuggestedRisk; set => SetProperty(ref _liveSuggestedRisk, value); }
        public string LiveRecommendedAction { get => _liveRecommendedAction; set => SetProperty(ref _liveRecommendedAction, value); }
        public bool IsRepeatOffenderAlert { get => _isRepeatOffenderAlert; set => SetProperty(ref _isRepeatOffenderAlert, value); }
        public string RepeatAlertMessage { get => _repeatAlertMessage; set => SetProperty(ref _repeatAlertMessage, value); }
        public bool IsStudentSuggestionsOpen { get => _isStudentSuggestionsOpen; set => SetProperty(ref _isStudentSuggestionsOpen, value); }

        // Modal Commands
        public ICommand SaveModalCommand { get; private set; } = null!;
        public ICommand CancelModalCommand { get; private set; } = null!;
        public ICommand ToggleEditModeCommand { get; private set; } = null!;
        public ICommand CancelEditModeCommand { get; private set; } = null!;
        public ICommand DeleteCurrentModalIncidentCommand { get; private set; } = null!;
        public ICommand InvestigateCurrentModalIncidentCommand { get; private set; } = null!;
        public ICommand ApplyRuleRecommendationsCommand { get; private set; } = null!;
        public ICommand SelectStudentSuggestionCommand { get; private set; } = null!;

        private void InitializeModalCommands()
        {
            SaveModalCommand = new RelayCommand(async () => await SaveModalAsync());
            CancelModalCommand = new RelayCommand(() => IsIncidentModalOpen = false);
            ToggleEditModeCommand = new RelayCommand(() => IsModalEditing = !IsModalEditing);
            CancelEditModeCommand = new RelayCommand(() => IsModalEditing = false);
            DeleteCurrentModalIncidentCommand = new RelayCommand(async () => await DeleteCurrentModalIncidentAsync(), () => CanDeleteModalIncident);
            InvestigateCurrentModalIncidentCommand = new RelayCommand(async () => await InvestigateCurrentModalIncidentAsync());
            ApplyRuleRecommendationsCommand = new RelayCommand(ApplyRecommendations);
            SelectStudentSuggestionCommand = new RelayCommand(param => SelectStudentSuggestion(param as Student));
        }

        public void OpenCreateModalForStudent(string studentName)
        {
            OpenCreateModal();
            if (!string.IsNullOrWhiteSpace(studentName))
            {
                var match = _allEnrolledStudents.FirstOrDefault(s => s.FullName.Equals(studentName, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Equals(studentName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    SelectStudentSuggestion(match);
                }
                else
                {
                    ModalStudentName = studentName;
                }
            }
        }

        private async void OpenCreateModal()
        {
            ModalErrorMessage = string.Empty;
            IsModalNewRecord = true;
            IsModalEditing = true;
            _modalIncidentPk = 0;

            ModalIncidentNumber = await _ruleEngineService.GenerateIncidentNumberAsync();
            ModalTitle = $"Report New Incident ({ModalIncidentNumber})";
            ModalTitleField = string.Empty;
            ModalCategory = "General Misconduct";
            ModalDescription = string.Empty;
            ModalLocation = "Campus Grounds";
            ModalDate = DateTime.Today;
            ModalTime = DateTime.Now.ToString("HH:mm");
            ModalStudentName = string.Empty;
            ModalStudentNumber = string.Empty;
            ModalGradeSection = string.Empty;
            ModalParentName = string.Empty;
            ModalParentEmail = string.Empty;
            ModalReporter = !string.IsNullOrWhiteSpace(SessionManager.FullName) ? SessionManager.FullName : SessionManager.Username;
            ModalStatus = IncidentStatus.Reported;
            ModalSeverity = IncidentSeverity.Low;
            ModalRisk = RiskLevel.Low;
            ModalActionTaken = string.Empty;
            ModalNotes = string.Empty;
            SendParentNotification = true;
            NotificationStatusText = "Pending Save";

            IsIncidentModalOpen = true;
            TriggerLiveAnalysis();
        }

        public void OpenViewModal(Incident inc)
        {
            PopulateModalFromIncident(inc);
            IsModalNewRecord = false;
            IsModalEditing = false;
            ModalTitle = $"Incident Details: {inc.IncidentNumber}";
            IsIncidentModalOpen = true;
        }

        public void OpenEditModal(Incident inc)
        {
            PopulateModalFromIncident(inc);
            IsModalNewRecord = false;
            IsModalEditing = true;
            ModalTitle = $"Edit Incident: {inc.IncidentNumber}";
            IsIncidentModalOpen = true;
        }

        private void PopulateModalFromIncident(Incident inc)
        {
            ModalErrorMessage = string.Empty;
            _modalIncidentPk = inc.Id;
            ModalIncidentNumber = inc.IncidentNumber;
            ModalTitleField = inc.Title;
            ModalCategory = inc.Category;
            ModalDescription = inc.Description;
            ModalLocation = inc.Location;
            ModalDate = inc.IncidentDate;
            ModalTime = inc.IncidentDate.ToString("HH:mm");
            ModalStudentName = inc.PrimaryInvolvedStudent;
            ModalReporter = inc.ReportingStaffTeacher;
            ModalStatus = inc.Status;
            ModalSeverity = inc.Severity;
            ModalRisk = inc.Risk;
            ModalActionTaken = inc.ActionTaken;
            ModalNotes = inc.Notes;

            // Retrieve enrolled student details if matching
            var student = _allEnrolledStudents.FirstOrDefault(s => s.FullName.Equals(inc.PrimaryInvolvedStudent, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Equals(inc.PrimaryInvolvedStudent, StringComparison.OrdinalIgnoreCase) || s.Id == inc.StudentId);
            if (student != null)
            {
                ModalStudentNumber = student.StudentNumber;
                ModalGradeSection = $"{student.GradeLevel} - {student.Section}";
                ModalParentName = student.GuardianName;
                ModalParentEmail = student.GuardianEmail;
            }
            else
            {
                ModalStudentNumber = string.Empty;
                ModalGradeSection = string.Empty;
                ModalParentName = string.Empty;
                ModalParentEmail = string.Empty;
            }

            SendParentNotification = false;
            NotificationStatusText = "Stored";
            TriggerLiveAnalysis();
        }

        private async Task LoadEnrolledStudentsAsync()
        {
            try
            {
                _allEnrolledStudents = await _studentService.GetAllStudentsAsync();
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

            var matches = _allEnrolledStudents
                .Where(s => s.FullName.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                            s.StudentNumber.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .ToList();

            StudentSuggestions.Clear();
            foreach (var m in matches)
            {
                StudentSuggestions.Add(m);
            }

            IsStudentSuggestionsOpen = matches.Count > 0 && !matches.Any(m => m.FullName.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void SelectStudentSuggestion(Student? student)
        {
            if (student == null) return;

            ModalStudentName = student.FullName;
            ModalStudentNumber = student.StudentNumber;
            ModalGradeSection = $"{student.GradeLevel} - {student.Section}";
            ModalParentName = student.GuardianName;
            ModalParentEmail = student.GuardianEmail;

            StudentSuggestions.Clear();
            IsStudentSuggestionsOpen = false;
            TriggerLiveAnalysis();
        }

        private async void TriggerLiveAnalysis()
        {
            string narrative = $"{ModalTitleField} {ModalDescription}";
            var result = await _ruleEngineService.AnalyzeAsync(narrative, ModalLocation, ModalStudentName, ModalCategory);

            LiveSuggestedCategory = result.SuggestedCategory;
            LiveSuggestedSeverity = result.SuggestedSeverity;
            LiveSuggestedRisk = result.SuggestedRisk;
            LiveRecommendedAction = result.RecommendedAction;
            IsRepeatOffenderAlert = result.IsRepeatOffender;
            RepeatAlertMessage = result.RepeatAlertMessage;

            TriggeredRules.Clear();
            foreach (var r in result.TriggeredRules)
            {
                TriggeredRules.Add(r);
            }
        }

        private void ApplyRecommendations()
        {
            ModalCategory = LiveSuggestedCategory;
            ModalSeverity = LiveSuggestedSeverity;
            ModalRisk = LiveSuggestedRisk;
            if (!string.IsNullOrWhiteSpace(LiveRecommendedAction))
            {
                ModalActionTaken = string.IsNullOrEmpty(ModalActionTaken)
                    ? LiveRecommendedAction
                    : $"{ModalActionTaken}\n[Rule Recommendation]: {LiveRecommendedAction}";
            }
        }

        private async Task SaveModalAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalTitleField))
            {
                ModalErrorMessage = "Incident Title is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ModalStudentName))
            {
                ModalErrorMessage = "Primary involved student is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ModalDescription))
            {
                ModalErrorMessage = "Incident narrative description is required.";
                return;
            }

            try
            {
                // Match student record
                var matchedStudent = _allEnrolledStudents.FirstOrDefault(s => s.FullName.Equals(ModalStudentName.Trim(), StringComparison.OrdinalIgnoreCase) || s.StudentNumber.Equals(ModalStudentNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                DateTime incidentDateTime = ModalDate.Date;
                if (TimeSpan.TryParse(ModalTime, out var timeOfDay))
                {
                    incidentDateTime = incidentDateTime.Add(timeOfDay);
                }

                if (IsModalNewRecord)
                {
                    var newInc = new Incident
                    {
                        IncidentNumber = ModalIncidentNumber,
                        Title = ModalTitleField.Trim(),
                        Category = ModalCategory,
                        Description = ModalDescription.Trim(),
                        Location = ModalLocation.Trim(),
                        IncidentDate = incidentDateTime,
                        PrimaryInvolvedStudent = ModalStudentName.Trim(),
                        StudentId = matchedStudent?.Id,
                        ReportingStaffTeacher = ModalReporter.Trim(),
                        CreatedBy = SessionManager.Username,
                        Status = ModalStatus,
                        Severity = ModalSeverity,
                        Risk = ModalRisk,
                        ActionTaken = ModalActionTaken?.Trim() ?? string.Empty,
                        Notes = ModalNotes?.Trim() ?? string.Empty,
                        RecommendedAction = LiveRecommendedAction,
                        IsRepeatIncident = IsRepeatOffenderAlert
                    };

                    await _incidentService.AddIncidentAsync(newInc);

                    // Send Email Notification if checked and email is provided
                    if (SendParentNotification && !string.IsNullOrWhiteSpace(ModalParentEmail))
                    {
                        string emailSubject = $"Official Incident Notification: [{newInc.IncidentNumber}] {newInc.Title}";
                        string emailBody = $"Dear {ModalParentName},\n\nPlease be informed that an incident concerning {ModalStudentName} was recorded today ({incidentDateTime:yyyy-MM-dd HH:mm}).\nCategory: {ModalCategory}\nSeverity: {ModalSeverity}\nDescription: {ModalDescription}\n\nPlease contact the School Discipline Office for further inquiry.\n\nSincerely,\n{ModalReporter}\nMatti National High School";

                        await _commService.SendEmailAsync(matchedStudent?.StudentNumber ?? "N/A", ModalParentName, ModalParentEmail, emailSubject, emailBody);
                    }
                }
                else
                {
                    var existing = await _incidentService.GetIncidentByIdAsync(_modalIncidentPk);
                    if (existing != null)
                    {
                        existing.Title = ModalTitleField.Trim();
                        existing.Category = ModalCategory;
                        existing.Description = ModalDescription.Trim();
                        existing.Location = ModalLocation.Trim();
                        existing.IncidentDate = incidentDateTime;
                        existing.PrimaryInvolvedStudent = ModalStudentName.Trim();
                        if (matchedStudent != null) existing.StudentId = matchedStudent.Id;
                        existing.ReportingStaffTeacher = ModalReporter.Trim();
                        existing.Status = ModalStatus;
                        existing.Severity = ModalSeverity;
                        existing.Risk = ModalRisk;
                        existing.ActionTaken = ModalActionTaken?.Trim() ?? string.Empty;
                        existing.Notes = ModalNotes?.Trim() ?? string.Empty;

                        await _incidentService.UpdateIncidentAsync(existing);
                    }
                }

                IsIncidentModalOpen = false;
                await LoadIncidentsAsync();
            }
            catch (Exception ex)
            {
                ModalErrorMessage = ex.Message;
            }
        }

        private async Task DeleteCurrentModalIncidentAsync()
        {
            if (_modalIncidentPk <= 0) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete incident '{ModalIncidentNumber}' ({ModalTitleField})?\nThis action cannot be undone.",
                "Confirm Delete Incident",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var inc = await _incidentService.GetIncidentByIdAsync(_modalIncidentPk);
                    if (inc != null)
                    {
                        await _incidentService.DeleteIncidentAsync(inc);
                    }
                    IsIncidentModalOpen = false;
                    SelectedIncident = null;
                    await LoadIncidentsAsync();
                }
                catch (Exception ex)
                {
                    ModalErrorMessage = $"Error deleting incident: {ex.Message}";
                }
            }
        }

        private async Task InvestigateCurrentModalIncidentAsync()
        {
            if (_modalIncidentPk <= 0) return;
            try
            {
                var target = await _incidentService.GetIncidentByIdAsync(_modalIncidentPk);
                if (target != null)
                {
                    IsIncidentModalOpen = false;
                    _openInvestigationAction?.Invoke(target);
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Error opening investigation: {ex.Message}";
            }
        }
    }
}
