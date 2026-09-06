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

        private string _selectedDateRange = "This Semester";
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
            RefreshCommand               = new RelayCommand(async _ => await LoadHeatmapDataAsync());
            ExportLocationReportCommand  = new RelayCommand(_ => ExportLocationReport());

            DateRanges = new List<string> { "This Month", "This Semester", "All Time" };
            Categories = new List<string> { "All Categories", "Bullying & Harassment", "Vandalism", "Substance Use", "Academic Dishonesty", "Physical Altercation" };

            _ = LoadHeatmapDataAsync();
        }

        public List<string> DateRanges { get; }
        public List<string> Categories { get; }

        public string SelectedDateRange
        {
            get => _selectedDateRange;
            set
            {
                if (SetProperty(ref _selectedDateRange, value))
                {
                    _ = LoadHeatmapDataAsync();
                }
            }
        }

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

        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

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
                var rawData = await _heatmapService.GetCampusLocationDataAsync(SelectedDateRange, SelectedCategory);
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
                        RiskBadgeColor = badgeColor
                    });
                }

                HeatmapNodes = new ObservableCollection<HeatmapNodeModel>(nodes);
                TopLocations = new ObservableCollection<HeatmapNodeModel>(nodes.OrderByDescending(n => n.IncidentCount).Take(6));
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