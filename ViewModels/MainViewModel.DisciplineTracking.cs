using SmartSchoolIncidentManagementSystem.Services;
using System.Windows;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class MainViewModel
    {
        public void ShowDisciplinaryActions()
        {
            if (!EnsureDisciplineAccess()) return;
            ActiveSection = "DisciplinaryActions";
            CurrentViewModel = new DisciplinaryActionsHubViewModel(
                ShowConsequenceTracking,
                ShowRestorativeInterventions,
                ShowSuspensionCalendar,
                ShowProbationTracking,
                ShowParentNotices,
                _consequenceService,
                _restorativeService,
                _suspensionService,
                _probationService,
                _templateService);
        }

        public void ShowConsequenceTracking()
        {
            if (!EnsureDisciplineAccess()) return;
            ActiveSection = "DisciplinaryActions";
            CurrentViewModel = new ConsequenceTrackingViewModel(
                _consequenceService, _incidentService, ShowDisciplinaryActions);
        }

        public void ShowRestorativeInterventions()
        {
            if (!EnsureDisciplineAccess()) return;
            ActiveSection = "DisciplinaryActions";
            CurrentViewModel = new RestorativeInterventionsViewModel(
                _restorativeService, _incidentService, ShowDisciplinaryActions);
        }

        public void ShowSuspensionCalendar()
        {
            if (!EnsureDisciplineAccess()) return;
            ActiveSection = "DisciplinaryActions";
            CurrentViewModel = new SuspensionCalendarViewModel(
                _suspensionService, _incidentService, ShowDisciplinaryActions);
        }

        public void ShowProbationTracking()
        {
            if (!EnsureDisciplineAccess()) return;
            ActiveSection = "DisciplinaryActions";
            CurrentViewModel = new ProbationTrackingViewModel(
                _probationService, _incidentService, ShowDisciplinaryActions);
        }

        private static bool EnsureDisciplineAccess()
        {
            if (DisciplinaryAuthorization.CanManage) return true;
            MessageBox.Show(
                "Only personnel authorized under school policy may manage suspension and probation records.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
