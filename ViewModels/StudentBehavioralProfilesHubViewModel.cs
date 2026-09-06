using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    /// <summary>Hub ViewModel for the Student Behavioral Profiles 3-card overview.</summary>
    public class StudentBehavioralProfilesHubViewModel : ObservableObject
    {
        private readonly Action _onBehavioralTranscripts;
        private readonly Action _onConductAnalytics;
        private readonly Action _onIndividualActionPlans;
        private readonly Action? _onAnecdotalRecords;

        public StudentBehavioralProfilesHubViewModel(
            Action onBehavioralTranscripts,
            Action onConductAnalytics,
            Action onIndividualActionPlans,
            Action? onAnecdotalRecords = null)
        {
            _onBehavioralTranscripts  = onBehavioralTranscripts  ?? throw new ArgumentNullException(nameof(onBehavioralTranscripts));
            _onConductAnalytics       = onConductAnalytics        ?? throw new ArgumentNullException(nameof(onConductAnalytics));
            _onIndividualActionPlans  = onIndividualActionPlans   ?? throw new ArgumentNullException(nameof(onIndividualActionPlans));
            _onAnecdotalRecords       = onAnecdotalRecords;

            var openTranscriptsCmd = new RelayCommand(_ => _onBehavioralTranscripts());
            var openAnalyticsCmd   = new RelayCommand(_ => _onConductAnalytics());
            var openPlansCmd       = new RelayCommand(_ => _onIndividualActionPlans());
            var openAnecdotalCmd   = new RelayCommand(_ => _onAnecdotalRecords?.Invoke());

            NavigateBehavioralTranscriptsCommand = openTranscriptsCmd;
            OpenBehavioralTranscriptsCommand     = openTranscriptsCmd;

            NavigateConductAnalyticsCommand      = openAnalyticsCmd;
            OpenConductAnalyticsCommand          = openAnalyticsCmd;

            NavigateIndividualActionPlansCommand = openPlansCmd;
            OpenIndividualActionPlansCommand     = openPlansCmd;

            NavigateAnecdotalRecordsCommand      = openAnecdotalCmd;
            OpenAnecdotalRecordsCommand          = openAnecdotalCmd;
        }

        public ICommand NavigateBehavioralTranscriptsCommand { get; }
        public ICommand OpenBehavioralTranscriptsCommand { get; }

        public ICommand NavigateConductAnalyticsCommand { get; }
        public ICommand OpenConductAnalyticsCommand { get; }

        public ICommand NavigateIndividualActionPlansCommand { get; }
        public ICommand OpenIndividualActionPlansCommand { get; }

        public ICommand NavigateAnecdotalRecordsCommand { get; }
        public ICommand OpenAnecdotalRecordsCommand { get; }

        public bool IsTeacher => !string.IsNullOrEmpty(SessionManager.Role) &&
            (SessionManager.Role.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0 ||
             SessionManager.Role.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0);

        public string Title    => "Student Behavioral Profiles";
        public string Subtitle => IsTeacher 
            ? "Full behavioral histories, conduct trajectory analytics, and observation logs."
            : "Full behavioral histories, conduct trajectory analytics, and individualized action plans.";
    }
}