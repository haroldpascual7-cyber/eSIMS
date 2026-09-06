using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class DisciplinaryActionsHubViewModel : ObservableObject
    {
        private readonly IConsequenceService? _consequenceService;
        private readonly IRestorativeService? _restorativeService;
        private readonly ISuspensionService? _suspensionService;
        private readonly ILetterTemplateService? _letterService;

        private int _activeSanctionsCount = 0;
        private int _restorativePlansCount = 0;
        private int _activeSuspensionsCount = 0;
        private int _parentNoticesCount = 0;

        public DisciplinaryActionsHubViewModel(
            Action onOpenConsequenceTracking,
            Action onOpenRestorativeInterventions,
            Action onOpenSuspensionCalendar,
            Action? onOpenParentSummons = null,
            IConsequenceService? consequenceService = null,
            IRestorativeService? restorativeService = null,
            ISuspensionService? suspensionService = null,
            ILetterTemplateService? letterService = null)
        {
            _consequenceService = consequenceService;
            _restorativeService = restorativeService;
            _suspensionService = suspensionService;
            _letterService = letterService;

            OpenConsequenceTrackingCommand = new RelayCommand(onOpenConsequenceTracking);
            OpenRestorativeInterventionsCommand = new RelayCommand(onOpenRestorativeInterventions);
            OpenSuspensionCalendarCommand = new RelayCommand(onOpenSuspensionCalendar);
            OpenParentSummonsCommand = new RelayCommand(() => onOpenParentSummons?.Invoke());

            _ = LoadStatsAsync();
        }

        public ICommand OpenConsequenceTrackingCommand { get; }
        public ICommand OpenRestorativeInterventionsCommand { get; }
        public ICommand OpenSuspensionCalendarCommand { get; }
        public ICommand OpenParentSummonsCommand { get; }

        public int ActiveSanctionsCount
        {
            get => _activeSanctionsCount;
            set => SetProperty(ref _activeSanctionsCount, value);
        }

        public int RestorativePlansCount
        {
            get => _restorativePlansCount;
            set => SetProperty(ref _restorativePlansCount, value);
        }

        public int ActiveSuspensionsCount
        {
            get => _activeSuspensionsCount;
            set => SetProperty(ref _activeSuspensionsCount, value);
        }

        public int ParentNoticesCount
        {
            get => _parentNoticesCount;
            set => SetProperty(ref _parentNoticesCount, value);
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                if (_consequenceService != null)
                {
                    var logs = await _consequenceService.GetAllConsequenceLogsAsync();
                    ActiveSanctionsCount = logs.Count(l => l.Status == "Active" || l.Status == "Pending" || l.Status == "Under Review");
                }
                else
                {
                    ActiveSanctionsCount = 8;
                }

                if (_restorativeService != null)
                {
                    var plans = await _restorativeService.GetAllInterventionPlansAsync();
                    RestorativePlansCount = plans.Count(p => p.Status == "In Progress" || p.Status == "Pending Assignment");
                }
                else
                {
                    RestorativePlansCount = 5;
                }

                if (_suspensionService != null)
                {
                    var orders = await _suspensionService.GetAllSuspensionOrdersAsync();
                    ActiveSuspensionsCount = orders.Count(o => o.Status == "Active Ban" || !o.IsCleared);
                }
                else
                {
                    ActiveSuspensionsCount = 3;
                }

                if (_letterService != null)
                {
                    var templates = await _letterService.GetAllTemplatesAsync();
                    ParentNoticesCount = templates.Count;
                }
                else
                {
                    ParentNoticesCount = 6;
                }
            }
            catch
            {
                ActiveSanctionsCount = 8;
                RestorativePlansCount = 5;
                ActiveSuspensionsCount = 3;
                ParentNoticesCount = 6;
            }
        }
    }
}