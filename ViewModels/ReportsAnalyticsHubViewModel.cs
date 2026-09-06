using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ReportsAnalyticsHubViewModel : ObservableObject
    {
        private readonly Action _onCampusHeatmap;
        private readonly Action _onRecidivism;
        private readonly Action _onExecutiveReports;
        private readonly Action _onStudentInquiry;

        public ReportsAnalyticsHubViewModel(
            Action onCampusHeatmap,
            Action onRecidivism,
            Action onExecutiveReports,
            Action onStudentInquiry)
        {
            _onCampusHeatmap    = onCampusHeatmap    ?? throw new ArgumentNullException(nameof(onCampusHeatmap));
            _onRecidivism       = onRecidivism       ?? throw new ArgumentNullException(nameof(onRecidivism));
            _onExecutiveReports = onExecutiveReports ?? throw new ArgumentNullException(nameof(onExecutiveReports));
            _onStudentInquiry   = onStudentInquiry   ?? throw new ArgumentNullException(nameof(onStudentInquiry));

            var heatmapCmd    = new RelayCommand(_ => _onCampusHeatmap());
            var recidivismCmd = new RelayCommand(_ => _onRecidivism());
            var execCmd       = new RelayCommand(_ => _onExecutiveReports());
            var inquiryCmd    = new RelayCommand(_ => _onStudentInquiry());

            NavigateCampusHeatmapCommand    = heatmapCmd;
            OpenCampusHeatmapCommand        = heatmapCmd;

            NavigateRecidivismCommand       = recidivismCmd;
            OpenRecidivismCommand           = recidivismCmd;

            NavigateExecutiveReportsCommand = execCmd;
            OpenExecutiveReportsCommand     = execCmd;

            NavigateStudentInquiryCommand   = inquiryCmd;
            OpenStudentInquiryCommand       = inquiryCmd;
        }

        public ICommand NavigateCampusHeatmapCommand    { get; }
        public ICommand OpenCampusHeatmapCommand        { get; }

        public ICommand NavigateRecidivismCommand       { get; }
        public ICommand OpenRecidivismCommand           { get; }

        public ICommand NavigateExecutiveReportsCommand { get; }
        public ICommand OpenExecutiveReportsCommand     { get; }

        public ICommand NavigateStudentInquiryCommand   { get; }
        public ICommand OpenStudentInquiryCommand       { get; }

        public bool IsStaffOrTeacher => !string.IsNullOrEmpty(SessionManager.Role) &&
            (SessionManager.Role.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0 ||
             SessionManager.Role.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0);

        public bool CanAccessExecutiveReports => !IsStaffOrTeacher;

        public int CardColumns => IsStaffOrTeacher ? 3 : 4;

        public string Title    => "Reports & Safety Analytics Hub";
        public string Subtitle => "Campus danger heatmap, recidivism analytics, active student case monitoring, and safety reports.";
    }
}