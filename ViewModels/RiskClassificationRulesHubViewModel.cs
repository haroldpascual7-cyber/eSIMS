using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class RiskClassificationRulesHubViewModel : ObservableObject
    {
        public RiskClassificationRulesHubViewModel(
            Action onOpenKeywordMatrix,
            Action onOpenLocationWeighting,
            Action onOpenRepeatOffender)
        {
            OpenKeywordMatrixCommand = new RelayCommand(onOpenKeywordMatrix);
            OpenLocationWeightingCommand = new RelayCommand(onOpenLocationWeighting);
            OpenRepeatOffenderCommand = new RelayCommand(onOpenRepeatOffender);
        }

        public ICommand OpenKeywordMatrixCommand { get; }
        public ICommand OpenLocationWeightingCommand { get; }
        public ICommand OpenRepeatOffenderCommand { get; }
    }
}