using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class InvestigationDetailViewModel : ObservableObject
    {
        private readonly IInvestigationService _investigationService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onClose;
        private readonly Action _onOpenDisciplineTracking;

        private Incident _incident;
        private Investigation _investigation = new();
        private string _statusMessage = string.Empty;

        // Modal & Editing state
        private bool _isDisciplinaryModalOpen;
        private string _modalMessage = string.Empty;
        private DisciplinaryAction? _editingAction;
        private bool _isEditingAction;

        // Form properties for Add/Edit
        private string _actionFormType = "Parent Conference";
        private string _actionFormDetails = string.Empty;
        private DateTime _actionFormDate = DateTime.Now;
        private string _actionFormStatus = "Pending";
        private string _actionFormNotes = string.Empty;

        public InvestigationDetailViewModel(
            IInvestigationService investigationService,
            IIncidentService incidentService,
            Incident incident,
            Action onClose,
            Action onOpenDisciplineTracking)
        {
            _investigationService = investigationService ?? throw new ArgumentNullException(nameof(investigationService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _incident = incident ?? throw new ArgumentNullException(nameof(incident));
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
            _onOpenDisciplineTracking = onOpenDisciplineTracking ?? throw new ArgumentNullException(nameof(onOpenDisciplineTracking));

            DisciplinaryActions = new ObservableCollection<DisciplinaryAction>();

            FindingsOptions = new ObservableCollection<string>
            {
                "Pending",
                "Substantiated",
                "Unsubstantiated",
                "Inconclusive",
                "Dismissed"
            };

            ActionTypes = new ObservableCollection<string>
            {
                "Parent Conference",
                "Guidance Counseling Referral",
                "Behavioral Contract",
                "Written Reprimand",
                "Verbal Warning",
                "Restorative School Service"
            };

            ActionStatusOptions = new ObservableCollection<string>
            {
                "Pending",
                "In Progress",
                "Completed",
                "Waived"
            };

            SaveInvestigationCommand = new RelayCommand(async () => await SaveInvestigationAsync());
            SaveActionCommand = new RelayCommand(async () => await SaveActionAsync());
            AddActionCommand = SaveActionCommand;

            StartAddActionCommand = new RelayCommand(() => StartAddAction());
            StartEditActionCommand = new RelayCommand(param =>
            {
                if (param is DisciplinaryAction act) StartEditAction(act);
            });
            CancelEditCommand = new RelayCommand(() => CancelEdit());

            OpenDisciplinaryModalCommand = new RelayCommand(param =>
            {
                if (param is DisciplinaryAction act)
                    OpenDisciplinaryModal(act);
                else
                    OpenDisciplinaryModal(null);
            });
            CloseDisciplinaryModalCommand = new RelayCommand(() => CloseDisciplinaryModal());

            CompleteActionCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction act) await MarkActionCompletedAsync(act);
            });
            DeleteActionCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction act) await DeleteActionAsync(act);
            });

            SetStatusCommand = new RelayCommand(async param =>
            {
                if (param is IncidentStatus st) await ChangeIncidentStatusAsync(st);
            });

            CloseCommand = new RelayCommand(() => _onClose());
            OpenDisciplineTrackingCommand = new RelayCommand(() =>
            {
                IsLoading = false;
                IsRefreshing = false;
                _onOpenDisciplineTracking();
            });
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshAsync(), () => !IsLoading && !IsRefreshing);

            InitializeTimelineAndEvidence();
            InitializeObservationsAndFlags();
            InitializeDisciplinaryGuidanceCommands();
            _ = InitializeWorkspaceAsync();
        }

        public Incident Incident => _incident;
        public ObservableCollection<DisciplinaryAction> DisciplinaryActions { get; }
        public ObservableCollection<string> FindingsOptions { get; }
        public ObservableCollection<string> ActionTypes { get; }
        public ObservableCollection<string> ActionStatusOptions { get; }

        public string IncidentNumber => _incident.IncidentNumber;
        public string Title => _incident.Title;
        public string Category => _incident.Category;
        public RiskLevel Risk => _incident.Risk;
        public IncidentSeverity Severity => _incident.Severity;
        public IncidentStatus CurrentStatus => _incident.Status;

        public bool IsTeacher => !string.IsNullOrEmpty(SessionManager.Role) &&
            (SessionManager.Role.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0 ||
             SessionManager.Role.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0);
        public bool CanManageInvestigation => !IsTeacher;
        public bool IsReadOnly => IsTeacher;

        // Modal & Form State
        public bool IsDisciplinaryModalOpen
        {
            get => _isDisciplinaryModalOpen;
            set => SetProperty(ref _isDisciplinaryModalOpen, value);
        }

        public string ModalMessage
        {
            get => _modalMessage;
            set => SetProperty(ref _modalMessage, value);
        }

        public bool IsEditingAction
        {
            get => _isEditingAction;
            set
            {
                SetProperty(ref _isEditingAction, value);
                OnPropertyChanged(nameof(FormHeaderTitle));
                OnPropertyChanged(nameof(FormSubmitButtonText));
            }
        }

        public string FormHeaderTitle => IsEditingAction ? "✏️ Edit Disciplinary Action" : "➕ Record Corrective / Disciplinary Action";
        public string FormSubmitButtonText => IsEditingAction ? "💾 Save Changes" : "+ Add Action";

        public string ActionFormType
        {
            get => _actionFormType;
            set => SetProperty(ref _actionFormType, value);
        }

        public string ActionFormDetails
        {
            get => _actionFormDetails;
            set => SetProperty(ref _actionFormDetails, value);
        }

        public DateTime ActionFormDate
        {
            get => _actionFormDate;
            set => SetProperty(ref _actionFormDate, value);
        }

        public string ActionFormStatus
        {
            get => _actionFormStatus;
            set => SetProperty(ref _actionFormStatus, value);
        }

        public string ActionFormNotes
        {
            get => _actionFormNotes;
            set => SetProperty(ref _actionFormNotes, value);
        }

        // Backward-compatible properties
        public string NewActionType { get => ActionFormType; set => ActionFormType = value; }
        public string NewActionDetails { get => ActionFormDetails; set => ActionFormDetails = value; }
        public DateTime NewActionDate { get => ActionFormDate; set => ActionFormDate = value; }

        // Summary metrics
        public int CompletedActionsCount => DisciplinaryActions.Count(a => a.Status == "Completed");
        public int PendingActionsCount => DisciplinaryActions.Count(a => a.Status != "Completed");
        public bool HasDisciplinaryActions => DisciplinaryActions.Count > 0;

        // --- Free-text Lead Investigator (replaces old ComboBox / InvestigatorId) ---
        public string LeadInvestigatorName
        {
            get => _investigation.LeadInvestigatorName;
            set { _investigation.LeadInvestigatorName = value; OnPropertyChanged(); }
        }

        public DateTime StartDate
        {
            get => _investigation.StartDate;
            set { _investigation.StartDate = value; OnPropertyChanged(); }
        }

        public DateTime? TargetCompletionDate
        {
            get => _investigation.TargetCompletionDate;
            set { _investigation.TargetCompletionDate = value; OnPropertyChanged(); }
        }

        public string InvestigationNotes
        {
            get => _investigation.InvestigationNotes;
            set { _investigation.InvestigationNotes = value; OnPropertyChanged(); }
        }

        public string InterviewLogs
        {
            get => _investigation.InterviewLogs;
            set { _investigation.InterviewLogs = value; OnPropertyChanged(); }
        }

        public string WitnessStatements
        {
            get => _investigation.WitnessStatements;
            set { _investigation.WitnessStatements = value; OnPropertyChanged(); }
        }

        public string Findings
        {
            get => _investigation.Findings;
            set { _investigation.Findings = value; OnPropertyChanged(); }
        }

        public string FinalSummary
        {
            get => _investigation.FinalSummary;
            set { _investigation.FinalSummary = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Commands
        public ICommand SaveInvestigationCommand { get; }
        public ICommand SaveActionCommand { get; }
        public ICommand AddActionCommand { get; }
        public ICommand StartAddActionCommand { get; }
        public ICommand StartEditActionCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand OpenDisciplinaryModalCommand { get; }
        public ICommand CloseDisciplinaryModalCommand { get; }
        public ICommand CompleteActionCommand { get; }
        public ICommand DeleteActionCommand { get; }
        public ICommand SetStatusCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand OpenDisciplineTrackingCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task InitializeWorkspaceAsync()
        {
            _investigation = await _investigationService.GetOrCreateInvestigationAsync(_incident.Id);
            OnPropertyChanged(nameof(LeadInvestigatorName));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(TargetCompletionDate));
            OnPropertyChanged(nameof(InvestigationNotes));
            OnPropertyChanged(nameof(InterviewLogs));
            OnPropertyChanged(nameof(WitnessStatements));
            OnPropertyChanged(nameof(Findings));
            OnPropertyChanged(nameof(FinalSummary));
            OnPropertyChanged(nameof(IsFollowUpFlagged));
            OnPropertyChanged(nameof(FollowUpIssueType));
            OnPropertyChanged(nameof(FollowUpNotes));
            OnPropertyChanged(nameof(IsFollowUpResolved));
            OnPropertyChanged(nameof(HasActiveFollowUpFlag));
            OnPropertyChanged(nameof(FollowUpBadgeText));
            OnPropertyChanged(nameof(FollowUpSummaryDisplay));

            LoadTimelineEntries();
            await RefreshDisciplinaryActionsAsync();
        }

        public void OpenDisciplinaryModal(DisciplinaryAction? actionToEdit = null)
        {
            if (actionToEdit != null)
                StartEditAction(actionToEdit);
            else if (!IsEditingAction)
                StartAddAction();

            ModalMessage = string.Empty;
            IsDisciplinaryModalOpen = true;
        }

        public void CloseDisciplinaryModal()
        {
            IsDisciplinaryModalOpen = false;
            ModalMessage = string.Empty;
        }

        public void StartAddAction()
        {
            _editingAction = null;
            IsEditingAction = false;
            ActionFormType = "Parent Conference";
            ActionFormDetails = string.Empty;
            ActionFormDate = DateTime.Now;
            ActionFormStatus = "Pending";
            ActionFormNotes = string.Empty;
            ModalMessage = string.Empty;
        }

        public void StartEditAction(DisciplinaryAction action)
        {
            if (action == null) return;
            _editingAction = action;
            IsEditingAction = true;
            ActionFormType = action.ActionType ?? "Parent Conference";
            ActionFormDetails = action.ActionDetails ?? string.Empty;
            ActionFormDate = action.ScheduledDate;
            ActionFormStatus = string.IsNullOrWhiteSpace(action.Status) ? "Pending" : action.Status;
            ActionFormNotes = action.FollowUpNotes ?? string.Empty;
            ModalMessage = string.Empty;
            IsDisciplinaryModalOpen = true;
        }

        public void CancelEdit()
        {
            StartAddAction();
        }

        private async Task SaveInvestigationAsync()
        {
            // Validation: Lead Investigator must not be empty
            if (string.IsNullOrWhiteSpace(LeadInvestigatorName))
            {
                StatusMessage = "⚠ Please enter the lead investigator's name before saving.";
                return;
            }

            if (IsLoading) return;
            try
            {
                IsLoading = true;
                _investigation.LeadInvestigatorName = LeadInvestigatorName.Trim();
                await _investigationService.SaveInvestigationAsync(_investigation);
                StatusMessage = "✔ Investigation details saved successfully.";
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving investigation: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
