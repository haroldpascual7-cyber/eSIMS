using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class StudentInvestigationCaseItem : ObservableObject
    {
        public int IncidentId { get; set; }
        public string IncidentNumber { get; set; } = string.Empty;
        public string IncidentTitle { get; set; } = string.Empty;
        public string IncidentDescription { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public IncidentSeverity Severity { get; set; }
        public RiskLevel Risk { get; set; }
        public IncidentStatus IncidentStatus { get; set; }

        public string InvolvedPersons { get; set; } = string.Empty;
        public string Witnesses { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsRepeatIncident { get; set; }
        public int RepeatIncidentCount { get; set; }

        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Adviser { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;

        public bool HasInvestigationRecord { get; set; }
        public bool IsActiveInvestigation => IncidentStatus == IncidentStatus.UnderInvestigation;
        public string LeadInvestigator { get; set; } = string.Empty;
        public DateTime? InvestigationStartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public string Findings { get; set; } = "Pending";
        public string InvestigationNotes { get; set; } = string.Empty;
        public string InterviewLogs { get; set; } = string.Empty;
        public string WitnessStatements { get; set; } = string.Empty;
        public string ReportingParty { get; set; } = string.Empty;
        public string FinalSummary { get; set; } = string.Empty;

        public string StatusBadgeText => IsActiveInvestigation ? "Currently Under Investigation" : IncidentStatus.ToString();

        public bool IsCasePendingOrUnsolved =>
            IncidentStatus == IncidentStatus.Reported ||
            IncidentStatus == IncidentStatus.UnderInvestigation ||
            IncidentStatus == IncidentStatus.ActionTaken;

        public string CaseResolutionStatusText => IncidentStatus switch
        {
            IncidentStatus.UnderInvestigation => "PENDING / NOT SOLVED (UNDER ACTIVE INVESTIGATION)",
            IncidentStatus.Reported           => "PENDING / NOT SOLVED (AWAITING INVESTIGATION)",
            IncidentStatus.ActionTaken        => "PENDING RESOLUTION (INTERIM ACTION APPLIED)",
            IncidentStatus.Resolved           => "CASE SOLVED & RESOLVED",
            IncidentStatus.Closed             => "CASE CLOSED & ARCHIVED",
            _                                 => "STATUS UNCONFIRMED"
        };

        public string CaseResolutionDescription => IncidentStatus switch
        {
            IncidentStatus.UnderInvestigation => "Case is active and unresolved. Investigative inquiry and hearings are currently in progress.",
            IncidentStatus.Reported           => "Incident is logged and awaiting formal classification and lead investigator assignment.",
            IncidentStatus.ActionTaken        => "Interim disciplinary action has been executed; final case clearance is pending.",
            IncidentStatus.Resolved           => "All investigations concluded, findings substantiated/dismissed, and resolution executed.",
            IncidentStatus.Closed             => "Formal disciplinary proceeding is concluded and sealed in the student's institutional record.",
            _                                 => "Case awaiting administrative review."
        };

        public double MapPinX { get; set; } = 350;
        public double MapPinY { get; set; } = 200;
        public string MapZoneName { get; set; } = "Campus Grounds";
        public string MapZoneKey { get; set; } = "GROUNDS";
    }

    public class StudentInvestigationInquiryViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly IInvestigationService _investigationService;
        private readonly Action _onBack;

        private string _searchText = string.Empty;
        private string _selectedFilter = "All"; // "All", "Active", "Resolved"
        private string _selectedDossierTab = "MapOverview"; // "MapOverview" or "FullReport"
        private StudentInvestigationCaseItem? _selectedCase;
        private bool _isLoading;
        private int _totalCasesCount;
        private int _activeInvestigationsCount;

        public ObservableCollection<StudentInvestigationCaseItem> AllCases { get; } = new();
        public ObservableCollection<StudentInvestigationCaseItem> FilteredCases { get; } = new();

        private readonly int? _preselectedIncidentId;
        private readonly string? _preselectedIncidentNumber;

        public StudentInvestigationInquiryViewModel(
            IIncidentService incidentService,
            IInvestigationService investigationService,
            Action onBack,
            Incident? preselectedIncident = null)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _investigationService = investigationService ?? throw new ArgumentNullException(nameof(investigationService));
            _onBack = onBack ?? throw new ArgumentNullException(nameof(onBack));
            _preselectedIncidentId = preselectedIncident?.Id;
            _preselectedIncidentNumber = preselectedIncident?.IncidentNumber;

            BackCommand = new RelayCommand(_ => _onBack());
            RefreshCommand = new RelayCommand(async _ => await ExecuteRefreshAsync(), _ => !IsLoading && !IsRefreshing);
            FilterAllCommand = new RelayCommand(_ => { SelectedFilter = "All"; });
            FilterActiveCommand = new RelayCommand(_ => { SelectedFilter = "Active"; });
            FilterResolvedCommand = new RelayCommand(_ => { SelectedFilter = "Resolved"; });
            SwitchToMapOverviewCommand = new RelayCommand(_ => { SelectedDossierTab = "MapOverview"; });
            SwitchToFullReportCommand = new RelayCommand(_ => { SelectedDossierTab = "FullReport"; });

            SelectCaseCommand = new RelayCommand(param =>
            {
                if (param is StudentInvestigationCaseItem item)
                {
                    SelectedCase = item;
                }
            });

            _ = LoadCasesAsync();
        }

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FilterAllCommand { get; }
        public ICommand FilterActiveCommand { get; }
        public ICommand FilterResolvedCommand { get; }
        public ICommand SelectCaseCommand { get; }
        public ICommand SwitchToMapOverviewCommand { get; }
        public ICommand SwitchToFullReportCommand { get; }

        public string SelectedDossierTab
        {
            get => _selectedDossierTab;
            set
            {
                if (SetProperty(ref _selectedDossierTab, value))
                {
                    OnPropertyChanged(nameof(IsMapOverviewTab));
                    OnPropertyChanged(nameof(IsFullReportTab));
                }
            }
        }

        public bool IsMapOverviewTab => SelectedDossierTab == "MapOverview";
        public bool IsFullReportTab => SelectedDossierTab == "FullReport";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    OnPropertyChanged(nameof(IsFilterAll));
                    OnPropertyChanged(nameof(IsFilterActive));
                    OnPropertyChanged(nameof(IsFilterResolved));
                    ApplyFilter();
                }
            }
        }

        public bool IsFilterAll => SelectedFilter == "All";
        public bool IsFilterActive => SelectedFilter == "Active";
        public bool IsFilterResolved => SelectedFilter == "Resolved";

        public StudentInvestigationCaseItem? SelectedCase
        {
            get => _selectedCase;
            set
            {
                if (SetProperty(ref _selectedCase, value))
                {
                    OnPropertyChanged(nameof(HasSelectedCase));
                }
            }
        }

        public bool HasSelectedCase => SelectedCase != null;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonText));
                }
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonText));
                }
            }
        }

        public bool IsNotRefreshing => !IsRefreshing && !IsLoading;
        public string RefreshButtonText => (IsRefreshing || IsLoading) ? "⌛ Refreshing..." : "↻ Refresh Cases";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing || IsLoading) return;
            try
            {
                IsRefreshing = true;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
                await LoadCasesAsync();
                await Task.Delay(750);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing cases: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }

        public int TotalCasesCount
        {
            get => _totalCasesCount;
            set => SetProperty(ref _totalCasesCount, value);
        }

        public int ActiveInvestigationsCount
        {
            get => _activeInvestigationsCount;
            set => SetProperty(ref _activeInvestigationsCount, value);
        }

        public static (double X, double Y, string ZoneName, string ZoneKey) ResolveCampusCoordinates(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return (350, 200, "General Campus Grounds", "GROUNDS");

            var loc = location.Trim().ToLowerInvariant();

            if (loc.Contains("gym"))
                return (140, 110, "Gymnasium & Sports Complex", "GYM");

            if (loc.Contains("locker") || loc.Contains("restroom") || loc.Contains("toilet") || loc.Contains("cr"))
                return (330, 85, "Restrooms & Lockers", "LOCKERS");

            if (loc.Contains("science") || loc.Contains("lab") || loc.Contains("room") || loc.Contains("classroom") || loc.Contains("302"))
                return (530, 110, "Science & IT Lab Wing (Classrooms)", "SCIENCE");

            if (loc.Contains("cafe") || loc.Contains("canteen") || loc.Contains("dining") || loc.Contains("food"))
                return (320, 185, "Cafeteria & Canteen", "CAFETERIA");

            if (loc.Contains("court") || loc.Contains("playground") || loc.Contains("field") || loc.Contains("oval"))
                return (140, 310, "Covered Basketball Court & Playground", "COURT");

            if (loc.Contains("hall") || loc.Contains("lobby") || loc.Contains("corridor") || loc.Contains("detention"))
                return (330, 310, "Main Hallway & Lobby", "HALLWAY");

            if (loc.Contains("library") || loc.Contains("study") || loc.Contains("reading"))
                return (490, 280, "Library & Study Hall", "LIBRARY");

            if (loc.Contains("parking") || loc.Contains("gate") || loc.Contains("entrance") || loc.Contains("exit"))
                return (630, 330, "East Parking Lot", "PARKING");

            return (350, 200, location, "GROUNDS");
        }

        public async Task LoadCasesAsync()
        {
            IsLoading = true;
            try
            {
                var incidents = (await _incidentService.GetAllIncidentsAsync()).ToList();
                var investigations = (await _investigationService.GetAllInvestigationsAsync()).ToList();
                var invDict = investigations.GroupBy(i => i.IncidentId).ToDictionary(g => g.Key, g => g.First());

                var items = new List<StudentInvestigationCaseItem>();

                foreach (var inc in incidents)
                {
                    invDict.TryGetValue(inc.Id, out var inv);
                    var (pinX, pinY, zoneName, zoneKey) = ResolveCampusCoordinates(inc.Location);

                    var item = new StudentInvestigationCaseItem
                    {
                        IncidentId = inc.Id,
                        IncidentNumber = inc.IncidentNumber,
                        IncidentTitle = inc.Title,
                        IncidentDescription = inc.Description,
                        IncidentDate = inc.IncidentDate,
                        Location = string.IsNullOrWhiteSpace(inc.Location) ? "Campus Grounds" : inc.Location,
                        Category = inc.Category,
                        Severity = inc.Severity,
                        Risk = inc.Risk,
                        IncidentStatus = inc.Status,
                        InvolvedPersons = inc.InvolvedPersons ?? string.Empty,
                        Witnesses = inc.Witnesses ?? string.Empty,
                        RecommendedAction = inc.RecommendedAction ?? string.Empty,
                        ActionTaken = inc.ActionTaken ?? string.Empty,
                        Notes = inc.Notes ?? string.Empty,
                        IsRepeatIncident = inc.IsRepeatIncident,
                        RepeatIncidentCount = inc.RepeatIncidentCount,
                        StudentName = !string.IsNullOrWhiteSpace(inc.StudentDisplayName)
                            ? inc.StudentDisplayName
                            : (inc.Student != null ? $"{inc.Student.FirstName} {inc.Student.LastName}" : "Unknown Student"),
                        StudentNumber = inc.Student?.StudentNumber ?? "N/A",
                        GradeLevel = inc.Student?.GradeLevel ?? "N/A",
                        Section = inc.Student?.Section ?? "N/A",
                        Adviser = inc.Student?.Adviser ?? "N/A",
                        ProfilePicturePath = inc.Student?.ProfilePicturePath ?? string.Empty,
                        ReportingParty = !string.IsNullOrWhiteSpace(inc.ReportingStaffTeacher)
                            ? inc.ReportingStaffTeacher
                            : (inc.Reporter?.FullName ?? "School Staff"),
                        HasInvestigationRecord = inv != null,
                        LeadInvestigator = inv != null && !string.IsNullOrWhiteSpace(inv.LeadInvestigatorName)
                            ? inv.LeadInvestigatorName
                            : (inv?.Investigator?.FullName ?? (inc.Status == IncidentStatus.UnderInvestigation ? "Discipline Committee" : "N/A")),
                        InvestigationStartDate = inv?.StartDate,
                        TargetCompletionDate = inv?.TargetCompletionDate,
                        Findings = inv?.Findings ?? (inc.Status == IncidentStatus.Resolved ? "Concluded" : "Under Inquiry"),
                        InvestigationNotes = inv?.InvestigationNotes ?? string.Empty,
                        InterviewLogs = inv?.InterviewLogs ?? string.Empty,
                        WitnessStatements = inv?.WitnessStatements ?? string.Empty,
                        FinalSummary = inv?.FinalSummary ?? string.Empty,
                        MapPinX = pinX,
                        MapPinY = pinY,
                        MapZoneName = zoneName,
                        MapZoneKey = zoneKey
                    };

                    items.Add(item);
                }

                AllCases.Clear();
                foreach (var itm in items.OrderByDescending(i => i.IsActiveInvestigation).ThenByDescending(i => i.IncidentDate))
                {
                    AllCases.Add(itm);
                }

                TotalCasesCount = AllCases.Count;
                ActiveInvestigationsCount = AllCases.Count(c => c.IsActiveInvestigation);

                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            var query = AllCases.AsEnumerable();

            if (SelectedFilter == "Active")
            {
                query = query.Where(c => c.IsActiveInvestigation);
            }
            else if (SelectedFilter == "Resolved")
            {
                query = query.Where(c => c.IncidentStatus == IncidentStatus.Resolved || c.IncidentStatus == IncidentStatus.Closed);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    c.StudentName.ToLowerInvariant().Contains(term) ||
                    c.StudentNumber.ToLowerInvariant().Contains(term) ||
                    c.IncidentNumber.ToLowerInvariant().Contains(term) ||
                    c.IncidentTitle.ToLowerInvariant().Contains(term) ||
                    c.Section.ToLowerInvariant().Contains(term) ||
                    c.GradeLevel.ToLowerInvariant().Contains(term) ||
                    c.Location.ToLowerInvariant().Contains(term) ||
                    c.LeadInvestigator.ToLowerInvariant().Contains(term));
            }

            FilteredCases.Clear();
            foreach (var item in query)
            {
                FilteredCases.Add(item);
            }

            if (SelectedCase == null && (_preselectedIncidentId.HasValue || !string.IsNullOrWhiteSpace(_preselectedIncidentNumber)))
            {
                var match = FilteredCases.FirstOrDefault(c =>
                    (_preselectedIncidentId.HasValue && c.IncidentId == _preselectedIncidentId.Value) ||
                    (!string.IsNullOrWhiteSpace(_preselectedIncidentNumber) && c.IncidentNumber.Equals(_preselectedIncidentNumber, StringComparison.OrdinalIgnoreCase)))
                    ?? AllCases.FirstOrDefault(c =>
                    (_preselectedIncidentId.HasValue && c.IncidentId == _preselectedIncidentId.Value) ||
                    (!string.IsNullOrWhiteSpace(_preselectedIncidentNumber) && c.IncidentNumber.Equals(_preselectedIncidentNumber, StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                {
                    if (!FilteredCases.Contains(match))
                    {
                        FilteredCases.Insert(0, match);
                    }
                    SelectedCase = match;
                }
            }

            if (SelectedCase == null || !FilteredCases.Contains(SelectedCase))
            {
                SelectedCase = FilteredCases.FirstOrDefault();
            }
        }
    }
}
