using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class IncidentListViewModel
    {
        private void ToggleEditMode()
        {
            if (IsModalEditing)
            {
                IsModalEditing = false;
                return;
            }

            if (!CanEditModalIncident)
            {
                ModalErrorMessage = "Only the original Staff/Teacher reporter can edit a Reported incident. Status, severity, risk, and resolution updates require Administrator, Discipline Officer, or Guidance Counselor access.";
                return;
            }

            ModalErrorMessage = string.Empty;
            IsModalEditing = true;
        }

        private bool CanEditCurrentIncidentIntake()
        {
            if (IsModalNewRecord)
            {
                return CanManageIncidents;
            }

            if (CanManageOfficialIncidentFlow)
            {
                return true;
            }

            return IsLimitedIncidentReporterRole()
                && IsCurrentUserIncidentReporter()
                && _originalModalStatus == IncidentStatus.Reported;
        }

        private bool CanEditIncidentFromList(Incident incident)
        {
            if (CanManageOfficialIncidentFlow)
            {
                return true;
            }

            if (!IsLimitedIncidentReporterRole() || incident.Status != IncidentStatus.Reported)
            {
                return false;
            }

            return TextEquals(incident.CreatedBy, SessionManager.Username)
                || TextEquals(incident.ReportingStaffTeacher, SessionManager.Username)
                || TextEquals(incident.ReportingStaffTeacher, SessionManager.FullName);
        }

        private bool IsLimitedIncidentReporterRole()
        {
            var role = SessionManager.Role?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            if (role.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Discipline", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Guidance", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Counselor", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return role.Contains("Staff", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Teacher", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCurrentUserIncidentReporter()
        {
            return TextEquals(_modalCreatedBy, SessionManager.Username)
                || TextEquals(ModalReporter, SessionManager.Username)
                || TextEquals(ModalReporter, SessionManager.FullName);
        }

        private static bool TextEquals(string? left, string? right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void NotifyModalPermissionState()
        {
            OnPropertyChanged(nameof(IsModalReadOnly));
            OnPropertyChanged(nameof(IsIncidentIntakeReadOnly));
            OnPropertyChanged(nameof(IsReporterFieldReadOnly));
            OnPropertyChanged(nameof(IsOfficialFlowReadOnly));
            OnPropertyChanged(nameof(IsActionTakenReadOnly));
            OnPropertyChanged(nameof(CanDeleteModalIncident));
            OnPropertyChanged(nameof(CanInvestigateModalIncident));
            OnPropertyChanged(nameof(CanEditModalIncident));
            OnPropertyChanged(nameof(CanShowEditModalButton));
            OnPropertyChanged(nameof(CanEditIncidentIntake));
            OnPropertyChanged(nameof(CanManageOfficialIncidentFlow));
            OnPropertyChanged(nameof(CanEditOfficialIncidentFlow));
            OnPropertyChanged(nameof(CanEditActionTakenSummary));
            OnPropertyChanged(nameof(CanApplyRuleRecommendations));
            OnPropertyChanged(nameof(CanSendParentNotification));
            OnPropertyChanged(nameof(CanEditReporterField));
            OnPropertyChanged(nameof(CanCancelEdit));
            OnPropertyChanged(nameof(SaveModalButtonText));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
