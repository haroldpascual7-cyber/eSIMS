using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class InvestigationDetailViewModel
    {
        public bool IsGuidanceCounselor => !string.IsNullOrEmpty(SessionManager.Role) &&
            (SessionManager.Role.IndexOf("Guidance", StringComparison.OrdinalIgnoreCase) >= 0 ||
             SessionManager.Role.IndexOf("Counselor", StringComparison.OrdinalIgnoreCase) >= 0 ||
             SessionManager.Role.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0);

        public bool CanGuidanceResolve => IsGuidanceCounselor && CurrentStatus != IncidentStatus.Resolved && CurrentStatus != IncidentStatus.Closed;

        // Lifecycle Progression Properties for IncidentLifecycleProgressionControl
        public string CurrentLifecycleStage => CurrentStatus.ToString();

        public string LifecycleStatusLabel
        {
            get
            {
                return CurrentStatus switch
                {
                    IncidentStatus.Reported => "Stage 1: Reported",
                    IncidentStatus.UnderInvestigation => "Stage 2: Investigating",
                    IncidentStatus.ActionTaken => "Stage 3: Action Taken / Pending Guidance",
                    IncidentStatus.Resolved => "Stage 4: Resolved",
                    IncidentStatus.Closed => "Stage 5: Closed",
                    _ => "Investigation Progression"
                };
            }
        }

        public string LifecycleStageDescription
        {
            get
            {
                return CurrentStatus switch
                {
                    IncidentStatus.Reported => "1. Case is reported. Begin formal inquiry by assigning lead investigator.",
                    IncidentStatus.UnderInvestigation => "2. Active investigation in progress: documenting testimonies and evidence.",
                    IncidentStatus.ActionTaken => "3. Corrective disciplinary/counseling actions assigned (Pending). Awaiting Guidance Counselor completion.",
                    IncidentStatus.Resolved => "4. Guidance counseling, student restitution, and sanctions completed.",
                    IncidentStatus.Closed => "5. Case officially completed and locked.",
                    _ => "Incident lifecycle progression."
                };
            }
        }

        public ICommand ConfirmActionCompleteAndResolveCommand { get; private set; } = null!;
        public ICommand ResolveCaseFromActionCommand { get; private set; } = null!;

        private void InitializeDisciplinaryGuidanceCommands()
        {
            ConfirmActionCompleteAndResolveCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction action)
                {
                    await ConfirmActionAndResolveAsync(action);
                }
            });

            ResolveCaseFromActionCommand = new RelayCommand(async () =>
            {
                await ChangeIncidentStatusAsync(IncidentStatus.Resolved);
                StatusMessage = "✔ Case marked as RESOLVED following guidance counseling.";
            });
        }

        private async Task RefreshDisciplinaryActionsAsync()
        {
            DisciplinaryActions.Clear();
            var actions = await _investigationService.GetDisciplinaryActionsByIncidentIdAsync(_incident.Id);
            foreach (var act in actions) DisciplinaryActions.Add(act);
            OnPropertyChanged(nameof(CompletedActionsCount));
            OnPropertyChanged(nameof(PendingActionsCount));
            OnPropertyChanged(nameof(HasDisciplinaryActions));
            NotifyLifecycleProgression();
        }

        private void NotifyLifecycleProgression()
        {
            OnPropertyChanged(nameof(CurrentStatus));
            OnPropertyChanged(nameof(CurrentLifecycleStage));
            OnPropertyChanged(nameof(LifecycleStatusLabel));
            OnPropertyChanged(nameof(LifecycleStageDescription));
            OnPropertyChanged(nameof(CanGuidanceResolve));
        }

        private async Task SaveActionAsync()
        {
            if (string.IsNullOrWhiteSpace(ActionFormDetails))
            {
                ModalMessage = "⚠ Please provide action details or requirements.";
                StatusMessage = "⚠ Please provide action details.";
                return;
            }

            try
            {
                if (IsEditingAction && _editingAction != null)
                {
                    _editingAction.ActionType = ActionFormType;
                    _editingAction.ActionDetails = ActionFormDetails.Trim();
                    _editingAction.ScheduledDate = ActionFormDate;
                    _editingAction.Status = ActionFormStatus;
                    _editingAction.FollowUpNotes = ActionFormNotes?.Trim() ?? string.Empty;

                    if (ActionFormStatus == "Completed" && _editingAction.CompletedDate == null)
                        _editingAction.CompletedDate = DateTime.Now;
                    else if (ActionFormStatus != "Completed")
                        _editingAction.CompletedDate = null;

                    await _investigationService.UpdateDisciplinaryActionAsync(_editingAction);
                    await RefreshDisciplinaryActionsAsync();
                    ModalMessage = "✔ Disciplinary action updated successfully.";
                    StatusMessage = "Disciplinary action updated.";
                    StartAddAction();
                }
                else
                {
                    var action = new DisciplinaryAction
                    {
                        IncidentId = _incident.Id,
                        StudentId = _incident.StudentId,
                        ActionType = ActionFormType,
                        ActionDetails = ActionFormDetails.Trim(),
                        ScheduledDate = ActionFormDate,
                        Status = ActionFormStatus,
                        FollowUpNotes = ActionFormNotes?.Trim() ?? string.Empty,
                        CompletedDate = ActionFormStatus == "Completed" ? DateTime.Now : null
                    };

                    await _investigationService.AddDisciplinaryActionAsync(action);
                    await RefreshDisciplinaryActionsAsync();

                    _incident.Status = IncidentStatus.ActionTaken;
                    NotifyLifecycleProgression();
                    ModalMessage = "✔ Disciplinary action recorded successfully.";
                    StatusMessage = "Disciplinary action recorded.";
                    StartAddAction();
                }
            }
            catch (Exception ex)
            {
                ModalMessage = $"⚠ Error saving action: {ex.Message}";
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task MarkActionCompletedAsync(DisciplinaryAction action)
        {
            action.Status = "Completed";
            action.CompletedDate = DateTime.Now;
            await _investigationService.UpdateDisciplinaryActionAsync(action);
            await RefreshDisciplinaryActionsAsync();

            if (_editingAction != null && _editingAction.Id == action.Id)
            {
                ActionFormStatus = "Completed";
            }

            ModalMessage = "✔ Disciplinary action marked as completed.";
            StatusMessage = "Disciplinary action marked as completed.";
        }

        private async Task ConfirmActionAndResolveAsync(DisciplinaryAction action)
        {
            try
            {
                var confirm = MessageBox.Show(
                    $"Confirm counseling complete and mark incident '{IncidentNumber}' as RESOLVED?",
                    "Confirm Guidance Resolution",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                bool markResolved = (confirm == MessageBoxResult.Yes);
                string counselorUser = !string.IsNullOrWhiteSpace(SessionManager.FullName) ? SessionManager.FullName : SessionManager.Username;
                string note = $"Counseling confirmed complete by {counselorUser}.";

                await _investigationService.CompleteDisciplinaryActionAndResolveIncidentAsync(action.Id, note, markResolved);
                action.Status = "Completed";
                action.CompletedDate = DateTime.Now;

                if (markResolved)
                {
                    _incident.Status = IncidentStatus.Resolved;
                    NotifyLifecycleProgression();
                }

                await RefreshDisciplinaryActionsAsync();
                ModalMessage = markResolved
                    ? "✔ Action completed and case marked as RESOLVED."
                    : "✔ Action marked as completed.";
                StatusMessage = ModalMessage;
            }
            catch (Exception ex)
            {
                ModalMessage = $"⚠ Error: {ex.Message}";
            }
        }

        private async Task DeleteActionAsync(DisciplinaryAction action)
        {
            if (_editingAction != null && _editingAction.Id == action.Id)
            {
                StartAddAction();
            }

            await _investigationService.DeleteDisciplinaryActionAsync(action);
            await RefreshDisciplinaryActionsAsync();
            ModalMessage = "✔ Action removed.";
            StatusMessage = "Action removed.";
        }

        private async Task ChangeIncidentStatusAsync(IncidentStatus newStatus)
        {
            _incident.Status = newStatus;
            await _investigationService.UpdateIncidentStatusAsync(_incident.Id, newStatus);
            NotifyLifecycleProgression();
            StatusMessage = $"Case status updated to {newStatus}.";
        }
    }
}
