using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class DashboardViewModel
    {
        private List<Incident> _allCachedIncidents = new();
        private bool _isCampusOverviewActive = true;
        private bool _isMyCaseloadActive;

        public bool IsCampusOverviewActive
        {
            get => _isCampusOverviewActive;
            set
            {
                if (SetProperty(ref _isCampusOverviewActive, value))
                {
                    if (value)
                    {
                        _isMyCaseloadActive = false;
                        OnPropertyChanged(nameof(IsMyCaseloadActive));
                        ApplyScopeFilter();
                    }
                }
            }
        }

        public bool IsMyCaseloadActive
        {
            get => _isMyCaseloadActive;
            set
            {
                if (SetProperty(ref _isMyCaseloadActive, value))
                {
                    if (value)
                    {
                        _isCampusOverviewActive = false;
                        OnPropertyChanged(nameof(IsCampusOverviewActive));
                        ApplyScopeFilter();
                    }
                }
            }
        }

        public string CampusOverviewTabHeader => "🏫 Campus Overview";

        public string MyCaseloadTabHeader
        {
            get
            {
                var role = SessionManager.Role ?? string.Empty;
                if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) ||
                    role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return "👤 My Filed Reports";
                }
                if (role.Equals("Counselor", StringComparison.OrdinalIgnoreCase))
                {
                    return "👤 My Caseload / Consults";
                }
                return "👤 My Caseload";
            }
        }

        public string ScopeBadgeText => IsMyCaseloadActive
            ? $"Personal View: {SessionManager.FullName} ({SessionManager.Role})"
            : "Campus-Wide Safety Intelligence";

        public string CardTotalTitle => IsMyCaseloadActive ? "My Total Reports" : "Total Incidents";
        public string CardReportedTitle => IsMyCaseloadActive ? "My Pending Triage" : "Reported / Triage";
        public string CardReportedSubtitle => IsMyCaseloadActive ? "Awaiting admin intake" : "Pending review queue";
        public string CardInquiriesTitle => IsMyCaseloadActive ? "My Active Cases" : "Active Inquiries";
        public string CardInquiriesSubtitle => IsMyCaseloadActive ? "Under active follow-up" : "Under investigation";
        public string CardResolvedTitle => IsMyCaseloadActive ? "My Resolved Cases" : "Cases Resolved";
        public string CardResolvedSubtitle => IsMyCaseloadActive ? "Concluded resolution" : "Successfully concluded";
        public string CardCriticalTitle => IsMyCaseloadActive ? "My High Risk Cases" : "High / Critical Risk";
        public string CardCriticalSubtitle => IsMyCaseloadActive ? "Priority attention" : "Requires priority attention";

        public ICommand SelectCampusOverviewCommand { get; private set; } = null!;
        public ICommand SelectMyCaseloadCommand { get; private set; } = null!;

        public ICommand CardClickTotalCommand { get; private set; } = null!;
        public ICommand CardClickReportedCommand { get; private set; } = null!;
        public ICommand CardClickInquiriesCommand { get; private set; } = null!;
        public ICommand CardClickResolvedCommand { get; private set; } = null!;
        public ICommand CardClickCriticalCommand { get; private set; } = null!;

        private void InitializeCaseloadSupport()
        {
            SelectCampusOverviewCommand = new RelayCommand(() => IsCampusOverviewActive = true);
            SelectMyCaseloadCommand = new RelayCommand(() => IsMyCaseloadActive = true);

            CardClickTotalCommand = new RelayCommand(() =>
            {
                if (IsMyCaseloadActive)
                {
                    var kw = !string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : SessionManager.FullName;
                    NavigateTo(vm => vm.ShowIncidents(statusFilter: null, severityFilter: null, searchKeyword: kw));
                }
                else
                {
                    NavigateTo(vm => vm.ShowIncidents());
                }
            });

            CardClickReportedCommand = new RelayCommand(() =>
            {
                var kw = IsMyCaseloadActive ? (!string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : SessionManager.FullName) : null;
                NavigateTo(vm => vm.ShowIncidents(statusFilter: "Reported", severityFilter: null, searchKeyword: kw));
            });

            CardClickInquiriesCommand = new RelayCommand(() =>
            {
                if (IsMyCaseloadActive)
                {
                    var kw = !string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : SessionManager.FullName;
                    NavigateTo(vm => vm.ShowIncidents(statusFilter: "UnderInvestigation", severityFilter: null, searchKeyword: kw));
                }
                else
                {
                    NavigateTo(vm => vm.ShowInvestigations());
                }
            });

            CardClickResolvedCommand = new RelayCommand(() =>
            {
                var kw = IsMyCaseloadActive ? (!string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : SessionManager.FullName) : null;
                NavigateTo(vm => vm.ShowIncidents(statusFilter: "Resolved", severityFilter: null, searchKeyword: kw));
            });

            CardClickCriticalCommand = new RelayCommand(() =>
            {
                var kw = IsMyCaseloadActive ? (!string.IsNullOrWhiteSpace(SessionManager.Username) ? SessionManager.Username : SessionManager.FullName) : null;
                NavigateTo(vm => vm.ShowIncidents(statusFilter: null, severityFilter: "Critical", searchKeyword: kw));
            });
        }

        public void ProcessScopeData(List<Incident> all)
        {
            _allCachedIncidents = all;
            ApplyScopeFilter();
        }

        private void ApplyScopeFilter()
        {
            var userAccount = SessionManager.Username ?? string.Empty;
            var userFullName = SessionManager.FullName ?? string.Empty;

            List<Incident> displayed;
            if (IsMyCaseloadActive)
            {
                displayed = _allCachedIncidents.Where(i =>
                    (!string.IsNullOrWhiteSpace(i.CreatedBy) && i.CreatedBy.Equals(userAccount, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(i.ReportingStaffTeacher) &&
                     (i.ReportingStaffTeacher.Equals(userAccount, StringComparison.OrdinalIgnoreCase) ||
                      i.ReportingStaffTeacher.Equals(userFullName, StringComparison.OrdinalIgnoreCase))) ||
                    (i.Reporter != null &&
                     i.Reporter.FullName.Equals(userFullName, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                TotalIncidentsSubtitle = $"Assigned / filed by {SessionManager.FullName}";
            }
            else
            {
                displayed = _allCachedIncidents;
                TotalIncidentsSubtitle = "All safety reports";
            }

            TotalIncidents = displayed.Count;
            OpenIncidents = displayed.Count(i => i.Status == IncidentStatus.Reported);
            InProgressIncidents = displayed.Count(i => i.Status == IncidentStatus.UnderInvestigation || i.Status == IncidentStatus.ActionTaken);
            ResolvedIncidents = displayed.Count(i => i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Closed);
            CriticalIncidents = displayed.Count(i => i.Risk == RiskLevel.Critical || i.Risk == RiskLevel.High);
            NewIncidentCount = OpenIncidents;

            RecentIncidents.Clear();
            var recent = displayed.OrderByDescending(i => i.IncidentDate).Take(5);
            foreach (var inc in recent)
            {
                RecentIncidents.Add(inc);
            }

            OnPropertyChanged(nameof(CardTotalTitle));
            OnPropertyChanged(nameof(CardReportedTitle));
            OnPropertyChanged(nameof(CardReportedSubtitle));
            OnPropertyChanged(nameof(CardInquiriesTitle));
            OnPropertyChanged(nameof(CardInquiriesSubtitle));
            OnPropertyChanged(nameof(CardResolvedTitle));
            OnPropertyChanged(nameof(CardResolvedSubtitle));
            OnPropertyChanged(nameof(CardCriticalTitle));
            OnPropertyChanged(nameof(CardCriticalSubtitle));
            OnPropertyChanged(nameof(ScopeBadgeText));
            OnPropertyChanged(nameof(MyCaseloadTabHeader));
        }
    }
}
