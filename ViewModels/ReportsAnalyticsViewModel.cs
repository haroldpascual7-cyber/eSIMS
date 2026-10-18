using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class ReportsAnalyticsViewModel : ObservableObject
    {
        private readonly IHeatmapService _heatmapService;
        private readonly IRecidivismService _recidivismService;
        private readonly IPdfExportService _pdfService;
        private readonly ICommunicationService _commService;
        private readonly IIncidentService _incidentService;

        private int _selectedTabIndex;
        private bool _isLoading;

        // --- Heatmap State ---
        private string _selectedDateRange = "This Semester";
        private string _selectedHeatmapCategory = "All Categories";
        private ObservableCollection<HeatmapNodeModel> _heatmapNodes = new();
        private ObservableCollection<HeatmapNodeModel> _topLocations = new();

        // --- Recidivism & Repeat Alerts State ---
        private RecidivismSummaryModel _recidivismSummary = new();
        private ObservableCollection<ChronicOffenderWatchItem> _chronicWatchlist = new();
        private ChronicOffenderWatchItem? _selectedWatchItem;
        private ObservableCollection<Student> _allStudents = new();
        private Student? _selectedEnrolledStudent;
        private ObservableCollection<RepeatIncidentAlertItem> _repeatAlerts = new();
        private RepeatIncidentAlertItem? _selectedAlert;
        private ObservableCollection<RelatedIncidentHistoryItem> _selectedStudentHistory = new();
        private RelatedIncidentHistoryItem? _selectedTargetIncident;
        private string _selectedDetermination = "Guidance Counseling Referral";
        private string _followUpDocumentation = string.Empty;
        private string _followUpStatusMessage = string.Empty;

        // --- Executive Summary State ---
        private string _selectedPeriod = "Monthly";
        private string _selectedTimeframe = "August 2026";
        private string _recipientEmail = "principal@smartacademy.edu.ph";
        private ExecutiveReportFlags _flags = new();
        private ExecutiveReportData _reportData = new();
        private bool _isSendingEmail;

        // --- Incident History Summary State ---
        private ObservableCollection<Incident> _recentIncidents = new();
        private int _totalIncidentsCount;
        private int _highCriticalCount;
        private int _resolvedCount;

        private static readonly Dictionary<string, (double X, double Y)> _locationCoordinates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Gymnasium"] = (130, 95), ["Classrooms (Bldg A)"] = (335, 70), ["Restrooms (Bldg A)"] = (335, 70),
            ["Building A (Classrooms & Labs)"] = (335, 70), ["Science Labs"] = (545, 95), ["Science Lab"] = (545, 95),
            ["Cafeteria"] = (335, 167), ["Covered Court"] = (130, 310), ["Main Hallway"] = (335, 310),
            ["Library & Study"] = (510, 235), ["Library"] = (510, 235), ["East Parking Lot"] = (675, 310)
        };

        public ReportsAnalyticsViewModel(
            IHeatmapService heatmapService,
            IRecidivismService recidivismService,
            IPdfExportService pdfService,
            ICommunicationService commService,
            IIncidentService incidentService)
        {
            _heatmapService = heatmapService ?? throw new ArgumentNullException(nameof(heatmapService));
            _recidivismService = recidivismService ?? throw new ArgumentNullException(nameof(recidivismService));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _commService = commService ?? throw new ArgumentNullException(nameof(commService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));

            DateRanges = new() { "This Month", "This Semester", "All Time" };
            Categories = new() { "All Categories", "Bullying & Harassment", "Vandalism", "Substance Use", "Academic Dishonesty", "Physical Altercation" };
            PeriodOptions = new() { "Monthly", "Quarterly", "Annual" };
            TimeframeOptions = new() { "August 2026", "July 2026", "Q3 2026", "Q2 2026", "Academic Year 2025-2026" };
            DeterminationOptions = new() { "Guidance Counseling Referral", "Escalated Disciplinary Review", "Parent-Guardian Case Conference", "Behavioral Intervention Plan (IAP)", "Restorative Justice Circle", "Administrative Hearing" };

            // Commands
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshAsync(), () => !IsLoading && !IsRefreshing);
            ExportLocationReportCommand = new RelayCommand(ExportLocationReport);
            ExportRecidivismReportCommand = new RelayCommand(ExportRecidivismReport);
            ExportExecutivePdfCommand = new RelayCommand(ExportExecutivePdf);
            EmailExecutiveReportCommand = new RelayCommand(async () => await EmailExecutiveReportAsync());
            DocumentFollowUpActionCommand = new RelayCommand(async () => await DocumentFollowUpActionAsync());
            SelectAlertCommand = new RelayCommand(param => { if (param is RepeatIncidentAlertItem alert) SelectedAlert = alert; });
            SelectTargetIncidentCommand = new RelayCommand(param => { if (param is RelatedIncidentHistoryItem inc) SelectedTargetIncident = inc; });

            InitializeHeatmapCommands();
            _ = LoadAllReportsDataAsync();
        }

        public bool CanAccessExecutiveReports => SessionManager.HasPermission("System Administrator,Discipline Officer");

        public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }

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
        public string RefreshButtonText => (IsRefreshing || IsLoading) ? "⌛ Refreshing..." : "↻ Refresh Analytics";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing || IsLoading) return;
            try
            {
                IsRefreshing = true;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
                await LoadHeatmapAsync();
                await LoadRecidivismAsync();
                await LoadGeneralSummaryAsync();
                await Task.Delay(750);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Refresh analytics error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }

        // --- Heatmap Properties ---
        public List<string> DateRanges { get; }
        public List<string> Categories { get; }
        public string SelectedDateRange
        {
            get => _selectedDateRange;
            set { if (SetProperty(ref _selectedDateRange, value)) _ = LoadHeatmapAsync(); }
        }
        public string SelectedHeatmapCategory
        {
            get => _selectedHeatmapCategory;
            set { if (SetProperty(ref _selectedHeatmapCategory, value)) _ = LoadHeatmapAsync(); }
        }
        public ObservableCollection<HeatmapNodeModel> HeatmapNodes => _heatmapNodes;
        public ObservableCollection<HeatmapNodeModel> TopLocations => _topLocations;

        // --- Recidivism Properties ---
        public RecidivismSummaryModel RecidivismSummary { get => _recidivismSummary; set => SetProperty(ref _recidivismSummary, value); }
        public ObservableCollection<ChronicOffenderWatchItem> ChronicWatchlist => _chronicWatchlist;
        public ChronicOffenderWatchItem? SelectedWatchItem { get => _selectedWatchItem; set => SetProperty(ref _selectedWatchItem, value); }

        // --- Repeat-Incident Alerts Properties ---
        public ObservableCollection<Student> AllStudents => _allStudents;
        public Student? SelectedEnrolledStudent
        {
            get => _selectedEnrolledStudent;
            set { if (SetProperty(ref _selectedEnrolledStudent, value) && value != null) _ = OnStudentSelectedAsync(value); }
        }

        public ObservableCollection<RepeatIncidentAlertItem> RepeatAlerts => _repeatAlerts;
        public RepeatIncidentAlertItem? SelectedAlert
        {
            get => _selectedAlert;
            set
            {
                if (SetProperty(ref _selectedAlert, value))
                    _ = OnAlertSelectedAsync(value);
            }
        }
        public ObservableCollection<RelatedIncidentHistoryItem> SelectedStudentHistory => _selectedStudentHistory;
        public RelatedIncidentHistoryItem? SelectedTargetIncident
        {
            get => _selectedTargetIncident;
            set => SetProperty(ref _selectedTargetIncident, value);
        }
        public List<string> DeterminationOptions { get; }
        public string SelectedDetermination
        {
            get => _selectedDetermination;
            set => SetProperty(ref _selectedDetermination, value);
        }
        public string FollowUpDocumentation
        {
            get => _followUpDocumentation;
            set => SetProperty(ref _followUpDocumentation, value);
        }
        public string FollowUpStatusMessage
        {
            get => _followUpStatusMessage;
            set => SetProperty(ref _followUpStatusMessage, value);
        }

        public ICommand DocumentFollowUpActionCommand { get; }
        public ICommand SelectAlertCommand { get; }
        public ICommand SelectTargetIncidentCommand { get; }

        // --- Executive Properties ---
        public List<string> PeriodOptions { get; }
        public List<string> TimeframeOptions { get; }
        public string SelectedPeriod { get => _selectedPeriod; set => SetProperty(ref _selectedPeriod, value); }
        public string SelectedTimeframe { get => _selectedTimeframe; set => SetProperty(ref _selectedTimeframe, value); }
        public string RecipientEmail { get => _recipientEmail; set => SetProperty(ref _recipientEmail, value); }
        public ExecutiveReportFlags Flags { get => _flags; set => SetProperty(ref _flags, value); }
        public ExecutiveReportData ReportData { get => _reportData; set => SetProperty(ref _reportData, value); }
        public bool IsSendingEmail { get => _isSendingEmail; set => SetProperty(ref _isSendingEmail, value); }

        // --- General Incidents Summary ---
        public ObservableCollection<Incident> RecentIncidents => _recentIncidents;
        public int TotalIncidentsCount { get => _totalIncidentsCount; set => SetProperty(ref _totalIncidentsCount, value); }
        public int HighCriticalCount { get => _highCriticalCount; set => SetProperty(ref _highCriticalCount, value); }
        public int ResolvedCount { get => _resolvedCount; set => SetProperty(ref _resolvedCount, value); }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ExportLocationReportCommand { get; }
        public ICommand ExportRecidivismReportCommand { get; }
        public ICommand ExportExecutivePdfCommand { get; }
        public ICommand EmailExecutiveReportCommand { get; }

        public async Task LoadAllReportsDataAsync()
        {
            IsLoading = true;
            try
            {
                await LoadHeatmapAsync();
                await LoadRecidivismAsync();
                await LoadGeneralSummaryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAllReportsDataAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadHeatmapAsync()
        {
            var filters = new HeatmapFilterParams
            {
                TimeframeMode = SelectedTimeframeMode,
                SpecificDate = SelectedHeatmapDate,
                SelectedYear = SelectedYear,
                SelectedMonth = SelectedMonthNumber,
                CategoryFilter = SelectedHeatmapCategory
            };

            var raw = await _heatmapService.GetCampusLocationDataAsync(filters);
            _heatmapNodes.Clear();
            _topLocations.Clear();

            int max = raw.Any() ? raw.Max(x => x.TotalIncidents) : 1;
            if (max == 0) max = 1;

            foreach (var r in raw)
            {
                double x = 250.0;
                double y = 200.0;
                if (_locationCoordinates.TryGetValue(r.Location, out var coords))
                {
                    x = coords.Item1;
                    y = coords.Item2;
                }

                int count = r.TotalIncidents;
                double radius = count >= 8 ? 64 : (count >= 4 ? 46 : (count > 0 ? 34 : 26));
                string riskLevel = count >= 8 ? "High Concentration (8+)" : (count >= 4 ? "Moderate Activity (4-7)" : (count > 0 ? "Low / Stable Zone (1-3)" : "Zero Incidents Recorded"));
                string badgeColor = count >= 8 ? "#DC2626" : (count >= 4 ? "#D97706" : (count > 0 ? "#059669" : "#64748B"));

                Brush borderBrush = count >= 8 ? (Brush?)Application.Current?.TryFindResource("DangerBrush") ?? Brushes.Crimson :
                                    count >= 4 ? (Brush?)Application.Current?.TryFindResource("WarningBrush") ?? Brushes.DarkOrange :
                                    count >= 1 ? (Brush?)Application.Current?.TryFindResource("SuccessBrush") ?? Brushes.SeaGreen :
                                                 (Brush?)Application.Current?.TryFindResource("BorderBrush") ?? Brushes.LightGray;
                Brush textBrush = count >= 1 ? borderBrush : (Brush?)Application.Current?.TryFindResource("SecondaryTextBrush") ?? Brushes.SlateGray;

                var node = new HeatmapNodeModel
                {
                    LocationName = r.Location,
                    IncidentCount = count,
                    PrimaryInfraction = r.TopCategory,
                    Incidents = r.Incidents,
                    X = x,
                    Y = y,
                    Radius = radius,
                    RiskLevelText = riskLevel,
                    RiskBadgeColor = badgeColor,
                    NodeBorderBrush = borderBrush,
                    NodeTextBrush = textBrush
                };
                _heatmapNodes.Add(node);

                if (count > 0)
                {
                    _topLocations.Add(node);
                }
            }

            TotalFilteredIncidentsCount = _heatmapNodes.Sum(n => n.IncidentCount);
            OnPropertyChanged(nameof(TimeframeSummaryText));

            if (SelectedHeatmapLocation != null)
            {
                SelectedHeatmapLocation = _heatmapNodes.FirstOrDefault(n => n.LocationName.Equals(SelectedHeatmapLocation.LocationName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private async Task LoadRecidivismAsync()
        {
            RecidivismSummary = await _recidivismService.GetRecidivismSummaryAsync();
            var list = await _recidivismService.GetChronicOffendersAsync();
            _chronicWatchlist.Clear();
            foreach (var item in list) _chronicWatchlist.Add(item);

            var students = await _recidivismService.GetAllEnrolledStudentsAsync();
            _allStudents.Clear();
            foreach (var s in students) _allStudents.Add(s);

            var alerts = await _recidivismService.GetRepeatIncidentAlertsAsync();
            _repeatAlerts.Clear();
            foreach (var a in alerts) _repeatAlerts.Add(a);

            if (_repeatAlerts.Any())
            {
                var first = _repeatAlerts.First();
                _selectedAlert = first;
                OnPropertyChanged(nameof(SelectedAlert));
                await OnAlertSelectedAsync(first);
            }
        }

        private async Task OnAlertSelectedAsync(RepeatIncidentAlertItem? alert)
        {
            _selectedStudentHistory.Clear();
            FollowUpStatusMessage = string.Empty;
            FollowUpDocumentation = string.Empty;

            if (alert == null) return;

            var history = await _recidivismService.GetStudentRelatedIncidentsAsync(alert.StudentName, alert.EnrolledStudentId);
            foreach (var h in history) _selectedStudentHistory.Add(h);
            SelectedTargetIncident = _selectedStudentHistory.FirstOrDefault();
        }

        private async Task OnStudentSelectedAsync(Student student)
        {
            var alert = _repeatAlerts.FirstOrDefault(a => a.EnrolledStudentId == student.Id || a.StudentName.Equals(student.FullName, StringComparison.OrdinalIgnoreCase));
            if (alert != null)
            {
                SelectedAlert = alert;
            }
            else
            {
                _selectedStudentHistory.Clear();
                FollowUpStatusMessage = string.Empty;
                FollowUpDocumentation = string.Empty;

                var history = await _recidivismService.GetStudentRelatedIncidentsAsync(student.FullName, student.Id);
                foreach (var h in history) _selectedStudentHistory.Add(h);
                SelectedTargetIncident = _selectedStudentHistory.FirstOrDefault();

                SelectedAlert = new RepeatIncidentAlertItem
                {
                    StudentId = !string.IsNullOrEmpty(student.StudentNumber) ? student.StudentNumber : $"STD-{student.Id}",
                    StudentName = student.FullName,
                    GradeSection = $"{student.GradeLevel} - {student.Section}",
                    TotalIncidents = history.Count,
                    AlertLevel = history.Count >= 3 ? "High Alert (3+ Incidents)" : (history.Count == 2 ? "Review Needed (2 Incidents)" : "Standard Review (1 Incident)"),
                    TriggeredPattern = history.Any() ? $"{history.First().Category} Pattern" : "No Repeat Pattern Detected",
                    PrimaryCategory = history.Any() ? history.First().Category : "General Misconduct",
                    LastIncidentDate = history.Any() ? history.First().IncidentDate.ToString("yyyy-MM-dd") : "N/A",
                    EnrolledStudentId = student.Id
                };
            }
        }

        private async Task DocumentFollowUpActionAsync()
        {
            if (SelectedTargetIncident == null)
            {
                FollowUpStatusMessage = "⚠ Please select an incident from the history table to link follow-up action.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FollowUpDocumentation))
            {
                FollowUpStatusMessage = "⚠ Please enter documentation details describing the follow-up action.";
                return;
            }

            string user = SessionManager.CurrentUsername ?? "Discipline Officer";
            bool ok = await _recidivismService.DocumentRepeatAlertFollowUpAsync(
                SelectedTargetIncident.IncidentId,
                SelectedDetermination,
                FollowUpDocumentation,
                user);

            if (ok)
            {
                FollowUpStatusMessage = $"✔ Follow-up action linked to Incident #{SelectedTargetIncident.IncidentNumber} successfully.";
                FollowUpDocumentation = string.Empty;
                if (SelectedAlert != null)
                {
                    await OnAlertSelectedAsync(SelectedAlert);
                }
                await LoadGeneralSummaryAsync();
            }
            else
            {
                FollowUpStatusMessage = "⚠ Failed to link follow-up action. Please try again.";
            }
        }

        private async Task LoadGeneralSummaryAsync()
        {
            var all = await _incidentService.GetAllIncidentsAsync();
            var list = all.ToList();
            TotalIncidentsCount = list.Count;
            HighCriticalCount = list.Count(i => i.Severity == IncidentSeverity.High || i.Severity == IncidentSeverity.Critical);
            ResolvedCount = list.Count(i => i.Status == IncidentStatus.Resolved);

            _recentIncidents.Clear();
            foreach (var inc in list.Take(25)) _recentIncidents.Add(inc);
        }
    }
}
