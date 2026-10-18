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
    public class CampusHeatmapViewModel : ObservableObject
    {
        private readonly IHeatmapService _heatmapService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private string _selectedTimeframeMode = "This Month";
        private DateTime? _selectedHeatmapDate = DateTime.Today;
        private int _selectedYear = DateTime.Today.Year;
        private string _selectedMonthName = DateTime.Today.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
        private HeatmapNodeModel? _selectedHeatmapLocation;
        private int _totalFilteredIncidentsCount;
        private string _selectedCategory = "All Categories";
        private bool _isLoading;

        private ObservableCollection<HeatmapNodeModel> _heatmapNodes = new();
        private ObservableCollection<HeatmapNodeModel> _topLocations = new();

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

        public CampusHeatmapViewModel(
            IHeatmapService heatmapService,
            IPdfExportService pdfService,
            Action onBack)
        {
            _heatmapService = heatmapService ?? throw new ArgumentNullException(nameof(heatmapService));
            _pdfService     = pdfService     ?? throw new ArgumentNullException(nameof(pdfService));
            _onBack          = onBack          ?? throw new ArgumentNullException(nameof(onBack));

            BackToHubCommand             = new RelayCommand(_ => _onBack());
            RefreshCommand               = new RelayCommand(async _ => await ExecuteRefreshAsync(), _ => !IsLoading && !IsRefreshing);
            ExportLocationReportCommand  = new RelayCommand(_ => ExportLocationReport());

            SelectLocationCommand = new RelayCommand(param =>
            {
                if (param is HeatmapNodeModel node) SelectedHeatmapLocation = node;
                else if (param is string locName)
                {
                    var match = _heatmapNodes.FirstOrDefault(n => n.LocationName.Equals(locName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) SelectedHeatmapLocation = match;
                }
            });

            ClearSelectedLocationCommand = new RelayCommand(() => SelectedHeatmapLocation = null);

            TimeframeModes = new List<string>
            {
                "This Month",
                "Specific Date (Day)",
                "This Week",
                "Specific Month & Year",
                "Specific Year",
                "This Semester",
                "All Time"
            };

            AvailableYears = new List<int>
            {
                DateTime.Today.Year - 3,
                DateTime.Today.Year - 2,
                DateTime.Today.Year - 1,
                DateTime.Today.Year,
                DateTime.Today.Year + 1
            };

            AvailableMonths = new List<string>
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            };

            Categories = new List<string> { "All Categories", "Bullying & Harassment", "Vandalism", "Substance Use", "Academic Dishonesty", "Physical Altercation" };

            _ = LoadHeatmapDataAsync();
        }

        public List<string> TimeframeModes { get; }
        public List<int> AvailableYears { get; }
        public List<string> AvailableMonths { get; }
        public List<string> Categories { get; }

        public string SelectedTimeframeMode
        {
            get => _selectedTimeframeMode;
            set
            {
                if (SetProperty(ref _selectedTimeframeMode, value))
                {
                    OnPropertyChanged(nameof(IsDatePickVisible));
                    OnPropertyChanged(nameof(IsMonthPickVisible));
                    OnPropertyChanged(nameof(IsYearPickVisible));
                    OnPropertyChanged(nameof(TimeframeSummaryText));
                    _ = LoadHeatmapDataAsync();
                }
            }
        }

        public DateTime? SelectedHeatmapDate
        {
            get => _selectedHeatmapDate;
            set
            {
                if (SetProperty(ref _selectedHeatmapDate, value))
                {
                    OnPropertyChanged(nameof(TimeframeSummaryText));
                    _ = LoadHeatmapDataAsync();
                }
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    OnPropertyChanged(nameof(TimeframeSummaryText));
                    _ = LoadHeatmapDataAsync();
                }
            }
        }

        public string SelectedMonthName
        {
            get => _selectedMonthName;
            set
            {
                if (SetProperty(ref _selectedMonthName, value))
                {
                    OnPropertyChanged(nameof(TimeframeSummaryText));
                    _ = LoadHeatmapDataAsync();
                }
            }
        }

        public int SelectedMonthNumber
        {
            get
            {
                if (DateTime.TryParseExact(SelectedMonthName, "MMMM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                    return dt.Month;
                return DateTime.Today.Month;
            }
        }

        public bool IsDatePickVisible => SelectedTimeframeMode.Equals("Specific Date (Day)", StringComparison.OrdinalIgnoreCase);
        public bool IsMonthPickVisible => SelectedTimeframeMode.Equals("Specific Month & Year", StringComparison.OrdinalIgnoreCase);
        public bool IsYearPickVisible => SelectedTimeframeMode.Equals("Specific Month & Year", StringComparison.OrdinalIgnoreCase) ||
                                         SelectedTimeframeMode.Equals("Specific Year", StringComparison.OrdinalIgnoreCase);

        public int TotalFilteredIncidentsCount
        {
            get => _totalFilteredIncidentsCount;
            set => SetProperty(ref _totalFilteredIncidentsCount, value);
        }

        public string TimeframeSummaryText
        {
            get
            {
                if (SelectedTimeframeMode.Equals("Specific Date (Day)", StringComparison.OrdinalIgnoreCase))
                {
                    return SelectedHeatmapDate.HasValue
                        ? $"📅 Day: {SelectedHeatmapDate.Value:MMMM dd, yyyy} • {TotalFilteredIncidentsCount} Incidents"
                        : $"📅 Specific Date • {TotalFilteredIncidentsCount} Incidents";
                }
                if (SelectedTimeframeMode.Equals("Specific Month & Year", StringComparison.OrdinalIgnoreCase))
                {
                    return $"📅 Month: {SelectedMonthName} {SelectedYear} • {TotalFilteredIncidentsCount} Incidents";
                }
                if (SelectedTimeframeMode.Equals("Specific Year", StringComparison.OrdinalIgnoreCase))
                {
                    return $"📅 Year: {SelectedYear} • {TotalFilteredIncidentsCount} Incidents";
                }
                if (SelectedTimeframeMode.Equals("This Week", StringComparison.OrdinalIgnoreCase))
                {
                    return $"📅 Current Week • {TotalFilteredIncidentsCount} Incidents";
                }
                if (SelectedTimeframeMode.Equals("This Semester", StringComparison.OrdinalIgnoreCase))
                {
                    return $"📅 Current Semester • {TotalFilteredIncidentsCount} Incidents";
                }
                if (SelectedTimeframeMode.Equals("All Time", StringComparison.OrdinalIgnoreCase))
                {
                    return $"📅 All Time (Full History) • {TotalFilteredIncidentsCount} Incidents";
                }
                return $"📅 Past 30 Days (Rolling) • {TotalFilteredIncidentsCount} Incidents";
            }
        }

        public HeatmapNodeModel? SelectedHeatmapLocation
        {
            get => _selectedHeatmapLocation;
            set
            {
                if (SetProperty(ref _selectedHeatmapLocation, value))
                {
                    OnPropertyChanged(nameof(HasSelectedLocation));
                    OnPropertyChanged(nameof(SelectedLocationTitle));
                    OnPropertyChanged(nameof(SelectedLocationIncidents));
                }
            }
        }

        public bool HasSelectedLocation => SelectedHeatmapLocation != null;

        public string SelectedLocationTitle => SelectedHeatmapLocation != null
            ? $"{SelectedHeatmapLocation.LocationName} ({SelectedHeatmapLocation.IncidentCount} Incidents)"
            : "Campus Danger Overview";

        public ObservableCollection<Incident> SelectedLocationIncidents
        {
            get
            {
                var list = new ObservableCollection<Incident>();
                if (SelectedHeatmapLocation?.Incidents != null)
                {
                    foreach (var inc in SelectedHeatmapLocation.Incidents)
                        list.Add(inc);
                }
                return list;
            }
        }

        public ICommand SelectLocationCommand { get; }
        public ICommand ClearSelectedLocationCommand { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = LoadHeatmapDataAsync();
                }
            }
        }

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
        public string RefreshButtonText => (IsRefreshing || IsLoading) ? "⌛ Refreshing..." : "🔄 Refresh Data";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing || IsLoading) return;
            try
            {
                IsRefreshing = true;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
                await LoadHeatmapDataAsync();
                await Task.Delay(750);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing heatmap: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }

        public ObservableCollection<HeatmapNodeModel> HeatmapNodes { get => _heatmapNodes; set => SetProperty(ref _heatmapNodes, value); }
        public ObservableCollection<HeatmapNodeModel> TopLocations  { get => _topLocations; set => SetProperty(ref _topLocations, value); }

        public ICommand BackToHubCommand            { get; }
        public ICommand RefreshCommand              { get; }
        public ICommand ExportLocationReportCommand { get; }

        public async Task LoadHeatmapDataAsync()
        {
            IsLoading = true;
            try
            {
                var filterParams = new HeatmapFilterParams
                {
                    TimeframeMode = SelectedTimeframeMode,
                    SpecificDate = SelectedHeatmapDate,
                    SelectedYear = SelectedYear,
                    SelectedMonth = SelectedMonthNumber,
                    CategoryFilter = SelectedCategory
                };

                var rawData = await _heatmapService.GetCampusLocationDataAsync(filterParams);
                TotalFilteredIncidentsCount = rawData.Sum(r => r.TotalIncidents);
                OnPropertyChanged(nameof(TimeframeSummaryText));

                var nodes = new List<HeatmapNodeModel>();

                foreach (var raw in rawData)
                {
                    double x = 300;
                    double y = 200;
                    if (_locationCoordinates.TryGetValue(raw.Location, out var coords))
                    {
                        x = coords.X;
                        y = coords.Y;
                    }

                    int count = raw.TotalIncidents;
                    double radius = count >= 10 ? 64 : (count >= 5 ? 46 : 30);
                    
                    SolidColorBrush color = count >= 10 
                        ? new SolidColorBrush(Color.FromArgb(160, 220, 38, 38)) 
                        : (count >= 5 ? new SolidColorBrush(Color.FromArgb(160, 217, 119, 6)) : new SolidColorBrush(Color.FromArgb(160, 5, 150, 105)));

                    string riskLevel = count >= 10 ? "High Risk" : (count >= 5 ? "Moderate Risk" : "Low Risk");
                    string badgeColor = count >= 10 ? "#DC2626" : (count >= 5 ? "#D97706" : "#059669");

                    nodes.Add(new HeatmapNodeModel
                    {
                        LocationName = raw.Location,
                        X = x,
                        Y = y,
                        IncidentCount = count,
                        PrimaryInfraction = raw.TopCategory,
                        Radius = radius,
                        RiskColor = color,
                        RiskLevelText = riskLevel,
                        RiskBadgeColor = badgeColor,
                        Incidents = raw.Incidents
                    });
                }

                HeatmapNodes = new ObservableCollection<HeatmapNodeModel>(nodes);
                TopLocations = new ObservableCollection<HeatmapNodeModel>(nodes.OrderByDescending(n => n.IncidentCount).Take(6));

                if (SelectedHeatmapLocation != null)
                {
                    var updatedSelection = nodes.FirstOrDefault(n => n.LocationName.Equals(SelectedHeatmapLocation.LocationName, StringComparison.OrdinalIgnoreCase));
                    SelectedHeatmapLocation = updatedSelection;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportLocationReport()
        {
            if (!TopLocations.Any())
            {
                MessageBox.Show("No location data available to export.", "Export Report", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"Campus_Safety_Patrol_Report_{DateTime.Now:yyyy-MM-dd}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Campus Safety & Patrol Report PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateCampusSafetyHeatmapReport(dialog.FileName, TopLocations.ToList());
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting heatmap report:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}