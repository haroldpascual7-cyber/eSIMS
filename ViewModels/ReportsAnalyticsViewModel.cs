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

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ReportsAnalyticsViewModel : ObservableObject
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

        // --- Recidivism State ---
        private RecidivismSummaryModel _recidivismSummary = new();
        private ObservableCollection<ChronicOffenderWatchItem> _chronicWatchlist = new();
        private ChronicOffenderWatchItem? _selectedWatchItem;

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

        private readonly Dictionary<string, (double X, double Y)> _locationCoordinates = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Cafeteria", (320, 180) },
            { "Gymnasium", (140, 120) },
            { "Science Lab", (480, 110) },
            { "Main Hallway", (320, 280) },
            { "Covered Court", (140, 320) },
            { "Library", (480, 280) },
            { "Restrooms (Building A)", (220, 200) },
            { "East Parking Lot", (540, 340) }
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

            DateRanges = new List<string> { "This Month", "This Semester", "All Time" };
            Categories = new List<string> { "All Categories", "Bullying & Harassment", "Vandalism", "Substance Use", "Academic Dishonesty", "Physical Altercation" };
            PeriodOptions = new List<string> { "Monthly", "Quarterly", "Annual" };
            TimeframeOptions = new List<string> { "August 2026", "July 2026", "Q3 2026", "Q2 2026", "Academic Year 2025-2026" };

            // Commands
            RefreshCommand = new RelayCommand(async () => await LoadAllReportsDataAsync());
            ExportLocationReportCommand = new RelayCommand(ExportLocationReport);
            ExportRecidivismReportCommand = new RelayCommand(ExportRecidivismReport);
            ExportExecutivePdfCommand = new RelayCommand(ExportExecutivePdf);
            EmailExecutiveReportCommand = new RelayCommand(async () => await EmailExecutiveReportAsync());

            _ = LoadAllReportsDataAsync();
        }

        public bool CanAccessExecutiveReports => SessionManager.HasPermission("System Administrator,Discipline Officer");

        public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

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
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadHeatmapAsync()
        {
            var raw = await _heatmapService.GetCampusLocationDataAsync(SelectedDateRange, SelectedHeatmapCategory);
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
                double radius = count >= 10 ? 64 : (count >= 5 ? 46 : 30);
                string riskLevel = count >= 10 ? "High Risk" : (count >= 5 ? "Moderate Risk" : "Low Risk");
                string badgeColor = count >= 10 ? "#DC2626" : (count >= 5 ? "#D97706" : "#059669");

                var node = new HeatmapNodeModel
                {
                    LocationName = r.Location,
                    IncidentCount = count,
                    PrimaryInfraction = r.TopCategory,
                    X = x,
                    Y = y,
                    Radius = radius,
                    RiskLevelText = riskLevel,
                    RiskBadgeColor = badgeColor
                };
                _heatmapNodes.Add(node);
                _topLocations.Add(node);
            }
        }

        private async Task LoadRecidivismAsync()
        {
            RecidivismSummary = await _recidivismService.GetRecidivismSummaryAsync();
            var list = await _recidivismService.GetChronicOffendersAsync();
            _chronicWatchlist.Clear();
            foreach (var item in list) _chronicWatchlist.Add(item);
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

        private void ExportLocationReport()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Campus_Safety_Heatmap_{DateTime.Today:yyyyMMdd}.pdf");
                _pdfService.GenerateCampusSafetyHeatmapReport(path, _heatmapNodes.ToList());
                MessageBox.Show($"Campus Safety Heatmap exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Heatmap export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportRecidivismReport()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Recidivism_Watchlist_{DateTime.Today:yyyyMMdd}.pdf");
                _pdfService.GenerateRecidivismReport(path, RecidivismSummary, _chronicWatchlist.ToList());
                MessageBox.Show($"Recidivism Report exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Recidivism export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportExecutivePdf()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Executive_Summary_{SelectedTimeframe.Replace(' ', '_')}.pdf");
                _reportData.PeriodType = SelectedPeriod;
                _reportData.TimeframeText = SelectedTimeframe;
                _pdfService.GenerateExecutiveSummaryPdf(path, _reportData, _flags);
                MessageBox.Show($"Executive Safety Report exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Executive Report export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EmailExecutiveReportAsync()
        {
            if (string.IsNullOrWhiteSpace(RecipientEmail))
            {
                MessageBox.Show("Please enter a valid recipient email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsSendingEmail = true;
            try
            {
                string tempPdf = Path.Combine(Path.GetTempPath(), $"Executive_Summary_{Guid.NewGuid().ToString().Substring(0, 6)}.pdf");
                _reportData.PeriodType = SelectedPeriod;
                _reportData.TimeframeText = SelectedTimeframe;
                _pdfService.GenerateExecutiveSummaryPdf(tempPdf, _reportData, _flags);

                await _commService.SendExecutiveSummaryEmailAsync(tempPdf, RecipientEmail);
                MessageBox.Show($"Executive Report successfully dispatched to:\n{RecipientEmail}", "Dispatched", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send executive email: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSendingEmail = false;
            }
        }
    }
}
