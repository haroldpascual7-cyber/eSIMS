using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
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
    public partial class IncidentListViewModel
    {
        private IInvestigationService? _investigationService;
        private ObservableCollection<DisciplinaryAction> _disciplinaryActionsForModal = new();
        private string _counselingActionType = "Guidance Counseling Referral";
        private DateTime _counselingDate = DateTime.Today;
        private string _counselingStatus = "Pending";
        private string _counselingDetails = string.Empty;
        private string _counselingNotes = string.Empty;

        public ObservableCollection<DisciplinaryAction> DisciplinaryActionsForModal => _disciplinaryActionsForModal;
        public bool HasModalDisciplinaryActions => _disciplinaryActionsForModal.Count > 0;

        public ObservableCollection<string> ActionTypeOptions { get; } = new()
        {
            "Guidance Counseling Referral",
            "Parent Conference",
            "Behavioral Contract",
            "Restorative School Service",
            "In-School Suspension",
            "Out-of-School Suspension",
            "Behavioral Probation",
            "Verbal Warning",
            "Written Reprimand"
        };

        public ObservableCollection<string> ActionStatusOptions { get; } = new()
        {
            "Pending", "In Progress", "Completed", "Waived"
        };

        public string CounselingActionType
        {
            get => _counselingActionType;
            set => SetProperty(ref _counselingActionType, value);
        }

        public DateTime CounselingDate
        {
            get => _counselingDate;
            set => SetProperty(ref _counselingDate, value);
        }

        public string CounselingStatus
        {
            get => _counselingStatus;
            set => SetProperty(ref _counselingStatus, value);
        }

        public string CounselingDetails
        {
            get => _counselingDetails;
            set => SetProperty(ref _counselingDetails, value);
        }

        public string CounselingNotes
        {
            get => _counselingNotes;
            set => SetProperty(ref _counselingNotes, value);
        }

        // Lifecycle Progression Properties for IncidentLifecycleProgressionControl
        public string CurrentLifecycleStage => ModalStatus.ToString();

        public string LifecycleStatusLabel
        {
            get
            {
                return ModalStatus switch
                {
                    IncidentStatus.Reported => "Stage 1 of 5: Reported",
                    IncidentStatus.UnderInvestigation => "Stage 2 of 5: Under Investigation",
                    IncidentStatus.ActionTaken => "Stage 3 of 5: Action Taken / Pending Counseling",
                    IncidentStatus.Resolved => "Stage 4 of 5: Resolved",
                    IncidentStatus.Closed => "Stage 5 of 5: Closed",
                    _ => "Pipeline Tracker"
                };
            }
        }

        public string LifecycleStageDescription
        {
            get
            {
                return ModalStatus switch
                {
                    IncidentStatus.Reported => "1. Reported: Incident has been officially recorded. Awaiting initial investigation or corrective referral.",
                    IncidentStatus.UnderInvestigation => "2. Investigating: Evidence is being examined and witness testimony is being documented.",
                    IncidentStatus.ActionTaken => "3. Action Taken: Corrective/counseling referral is assigned with status 'Pending'. Awaiting Guidance Counselor completion.",
                    IncidentStatus.Resolved => "4. Resolved: Guidance counseling sessions, restitution, and sanctions are fulfilled.",
                    IncidentStatus.Closed => "5. Closed: Case has been officially concluded and archived.",
                    _ => "Interactive progression tracker."
                };
            }
        }

        public ICommand AddCounselingActionCommand { get; private set; } = null!;
        public ICommand ConfirmCounselingCompleteCommand { get; private set; } = null!;
        public ICommand ResolveIncidentFromCounselingCommand { get; private set; } = null!;
        public ICommand DeleteModalActionCommand { get; private set; } = null!;
        public ICommand SetStatusCommand { get; private set; } = null!;

        private void InitializeCounselingModalSupport(IInvestigationService? investigationService = null)
        {
            _investigationService = investigationService ?? new InvestigationService(new AppDbContext());

            AddCounselingActionCommand = new RelayCommand(async () => await AddCounselingActionAsync());

            ConfirmCounselingCompleteCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction action)
                    await ConfirmActionCompletedAsync(action);
            });

            ResolveIncidentFromCounselingCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction action)
                    await ResolveIncidentFromActionAsync(action);
            });

            DeleteModalActionCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction action)
                    await DeleteModalActionAsync(action);
            });

            SetStatusCommand = new RelayCommand(param =>
            {
                if (param is IncidentStatus st)
                {
                    if (CanEditOfficialIncidentFlow)
                    {
                        ModalStatus = st;
                        NotifyLifecycleProgression();
                    }
                    else
                    {
                        ModalErrorMessage = "Status changes require Administrator, Discipline Officer, or Guidance Counselor access.";
                    }
                }
            });
        }

        public void NotifyLifecycleProgression()
        {
            OnPropertyChanged(nameof(CurrentLifecycleStage));
            OnPropertyChanged(nameof(LifecycleStatusLabel));
            OnPropertyChanged(nameof(LifecycleStageDescription));
        }

        public async Task LoadDisciplinaryActionsForModalAsync(int incidentId)
        {
            _disciplinaryActionsForModal.Clear();
            if (incidentId <= 0 || _investigationService == null)
            {
                OnPropertyChanged(nameof(HasModalDisciplinaryActions));
                return;
            }

            try
            {
                var actions = await _investigationService.GetDisciplinaryActionsByIncidentIdAsync(incidentId);
                foreach (var a in actions)
                {
                    _disciplinaryActionsForModal.Add(a);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading actions: {ex.Message}");
            }

            OnPropertyChanged(nameof(HasModalDisciplinaryActions));
        }

        private async Task AddCounselingActionAsync()
        {
            if (string.IsNullOrWhiteSpace(CounselingDetails))
            {
                ModalErrorMessage = "Please specify requirements or counseling objectives before adding.";
                return;
            }

            if (_modalIncidentPk <= 0)
            {
                ModalErrorMessage = "Please save the incident first before attaching official disciplinary/counseling actions.";
                return;
            }

            try
            {
                var matchedStudent = _allEnrolledStudents.FirstOrDefault(s =>
                    s.FullName.Equals(ModalStudentName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    s.StudentNumber.Equals(ModalStudentNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                var action = new DisciplinaryAction
                {
                    IncidentId = _modalIncidentPk,
                    StudentId = matchedStudent?.Id,
                    ActionType = CounselingActionType,
                    ScheduledDate = CounselingDate,
                    Status = CounselingStatus,
                    ActionDetails = CounselingDetails.Trim(),
                    FollowUpNotes = CounselingNotes?.Trim() ?? string.Empty,
                    CompletedDate = CounselingStatus == "Completed" ? DateTime.Now : null
                };

                if (_investigationService != null)
                {
                    await _investigationService.AddDisciplinaryActionAsync(action);
                    await LoadDisciplinaryActionsForModalAsync(_modalIncidentPk);
                }

                // Transition Incident status to ActionTaken
                ModalStatus = IncidentStatus.ActionTaken;
                ModalActionTaken = $"[{CounselingActionType} - {CounselingStatus}] Scheduled: {CounselingDate:yyyy-MM-dd}. {CounselingDetails.Trim()}";
                NotifyLifecycleProgression();

                CounselingDetails = string.Empty;
                CounselingNotes = string.Empty;
                CounselingStatus = "Pending";
                ModalErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Error saving action: {ex.Message}";
            }
        }

        private async Task ConfirmActionCompletedAsync(DisciplinaryAction action)
        {
            try
            {
                if (_investigationService != null)
                {
                    string officer = !string.IsNullOrWhiteSpace(SessionManager.FullName) ? SessionManager.FullName : SessionManager.Username;
                    string note = $"Counseling confirmed complete by {officer} on {DateTime.Now:yyyy-MM-dd HH:mm}.";

                    await _investigationService.CompleteDisciplinaryActionAndResolveIncidentAsync(action.Id, note, markIncidentResolved: false);
                    await LoadDisciplinaryActionsForModalAsync(_modalIncidentPk);
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Error completing action: {ex.Message}";
            }
        }

        private async Task ResolveIncidentFromActionAsync(DisciplinaryAction action)
        {
            try
            {
                var confirm = MessageBox.Show(
                    $"Mark this incident as RESOLVED?\nThis confirms all guidance counseling and disciplinary sanctions have been fulfilled.",
                    "Resolve Incident",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    if (_investigationService != null)
                    {
                        string officer = !string.IsNullOrWhiteSpace(SessionManager.FullName) ? SessionManager.FullName : SessionManager.Username;
                        string note = $"Incident marked resolved following guidance counseling by {officer}.";

                        await _investigationService.CompleteDisciplinaryActionAndResolveIncidentAsync(action.Id, note, markIncidentResolved: true);
                        await LoadDisciplinaryActionsForModalAsync(_modalIncidentPk);
                    }

                    ModalStatus = IncidentStatus.Resolved;
                    NotifyLifecycleProgression();
                    await LoadIncidentsAsync();
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Error resolving incident: {ex.Message}";
            }
        }

        private async Task DeleteModalActionAsync(DisciplinaryAction action)
        {
            try
            {
                if (_investigationService != null)
                {
                    await _investigationService.DeleteDisciplinaryActionAsync(action);
                    await LoadDisciplinaryActionsForModalAsync(_modalIncidentPk);
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Error deleting action: {ex.Message}";
            }
        }
    }
}
