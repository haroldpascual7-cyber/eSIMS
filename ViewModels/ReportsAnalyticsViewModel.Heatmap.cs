using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class ReportsAnalyticsViewModel
    {
        private string _selectedTimeframeMode = "This Month";
        private DateTime? _selectedHeatmapDate = DateTime.Today;
        private int _selectedYear = DateTime.Today.Year;
        private string _selectedMonthName = DateTime.Today.ToString("MMMM", CultureInfo.InvariantCulture);
        private HeatmapNodeModel? _selectedHeatmapLocation;
        private int _totalFilteredIncidentsCount;

        public List<string> TimeframeModes { get; } = new()
        {
            "This Month",
            "Specific Date (Day)",
            "This Week",
            "Specific Month & Year",
            "Specific Year",
            "This Semester",
            "All Time"
        };

        public List<int> AvailableYears { get; } = new()
        {
            DateTime.Today.Year - 3,
            DateTime.Today.Year - 2,
            DateTime.Today.Year - 1,
            DateTime.Today.Year,
            DateTime.Today.Year + 1
        };

        public List<string> AvailableMonths { get; } = new()
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

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
                    _ = LoadHeatmapAsync();
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
                    _ = LoadHeatmapAsync();
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
                    _ = LoadHeatmapAsync();
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
                    _ = LoadHeatmapAsync();
                }
            }
        }

        public int SelectedMonthNumber
        {
            get
            {
                if (DateTime.TryParseExact(SelectedMonthName, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
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
            : "Select a campus location to inspect incidents";

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

        public ICommand? SelectLocationCommand { get; private set; }
        public ICommand? ClearSelectedLocationCommand { get; private set; }

        private void InitializeHeatmapCommands()
        {
            SelectLocationCommand = new RelayCommand(param =>
            {
                if (param is HeatmapNodeModel node)
                {
                    SelectedHeatmapLocation = node;
                }
                else if (param is string locName)
                {
                    var match = _heatmapNodes.FirstOrDefault(n => n.LocationName.Equals(locName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) SelectedHeatmapLocation = match;
                }
            });

            ClearSelectedLocationCommand = new RelayCommand(() =>
            {
                SelectedHeatmapLocation = null;
            });
        }
    }
}
