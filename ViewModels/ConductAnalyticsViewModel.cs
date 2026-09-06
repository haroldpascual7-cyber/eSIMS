using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ConductAnalyticsViewModel : ObservableObject
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly Action _onBack;
        private readonly Action<string> _onReviewStudent;

        private string _averageConductScoreText = "88.0/100";
        private string _highestRiskSection = "Grade 10 - St. Thomas";
        private string _activeInterventionsCount = "8";
        private string _escalationRateText = "+12.4% ↗";
        private string _escalationRateColor = "#DC2626";
        private bool _isLoading;

        private ISeries[] _trajectorySeries = Array.Empty<ISeries>();
        private Axis[] _trajectoryXAxes = Array.Empty<Axis>();
        private ISeries[] _categorySeries = Array.Empty<ISeries>();
        private Axis[] _categoryXAxes = Array.Empty<Axis>();
        private ObservableCollection<WatchlistStudentModel> _watchlistStudents = new();
        private WatchlistStudentModel? _selectedWatchlistStudent;

        public ConductAnalyticsViewModel(
            IAnalyticsService analyticsService,
            Action onBack,
            Action<string> onReviewStudent)
        {
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _onBack            = onBack            ?? throw new ArgumentNullException(nameof(onBack));
            _onReviewStudent   = onReviewStudent   ?? throw new ArgumentNullException(nameof(onReviewStudent));

            var backCmd = new RelayCommand(_ => _onBack());
            BackCommand = backCmd;
            BackToProfilesHubCommand = backCmd;

            ReviewStudentProfileCommand = new RelayCommand(param =>
            {
                string? studentId = param as string ?? SelectedWatchlistStudent?.StudentId;
                if (!string.IsNullOrWhiteSpace(studentId))
                {
                    _onReviewStudent(studentId);
                }
            });

            _ = LoadDashboardDataAsync();
        }

        public string AverageConductScoreText { get => _averageConductScoreText; set => SetProperty(ref _averageConductScoreText, value); }
        public string HighestRiskSection       { get => _highestRiskSection; set => SetProperty(ref _highestRiskSection, value); }
        public string ActiveInterventionsCount { get => _activeInterventionsCount; set => SetProperty(ref _activeInterventionsCount, value); }
        public string EscalationRateText       { get => _escalationRateText; set => SetProperty(ref _escalationRateText, value); }
        public string EscalationRateColor      { get => _escalationRateColor; set => SetProperty(ref _escalationRateColor, value); }
        public bool IsLoading                  { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public ISeries[] TrajectorySeries { get => _trajectorySeries; set => SetProperty(ref _trajectorySeries, value); }
        public Axis[] TrajectoryXAxes     { get => _trajectoryXAxes; set => SetProperty(ref _trajectoryXAxes, value); }
        public ISeries[] CategorySeries   { get => _categorySeries; set => SetProperty(ref _categorySeries, value); }
        public Axis[] CategoryXAxes       { get => _categoryXAxes; set => SetProperty(ref _categoryXAxes, value); }

        public ObservableCollection<WatchlistStudentModel> WatchlistStudents
        {
            get => _watchlistStudents;
            set => SetProperty(ref _watchlistStudents, value);
        }

        public WatchlistStudentModel? SelectedWatchlistStudent
        {
            get => _selectedWatchlistStudent;
            set => SetProperty(ref _selectedWatchlistStudent, value);
        }

        public ICommand BackCommand { get; }
        public ICommand BackToProfilesHubCommand { get; }
        public ICommand ReviewStudentProfileCommand { get; }

        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            try
            {
                var summary    = await _analyticsService.GetConductAnalyticsSummaryAsync();
                var trajectory = await _analyticsService.GetMonthlyTrajectoryAsync();
                var categories = await _analyticsService.GetCategoryBreakdownAsync();
                var watchlist  = await _analyticsService.GetWatchlistStudentsAsync();

                AverageConductScoreText  = $"{summary.AverageConductScore:F1}/100";
                HighestRiskSection        = summary.HighestRiskSection;
                ActiveInterventionsCount  = summary.ActiveInterventionsCount.ToString();
                EscalationRateText        = summary.EscalationRateText;
                EscalationRateColor       = summary.EscalationRateIncreased ? "#DC2626" : "#059669";

                // Setup Line Chart
                double[] trajValues = trajectory.Select(t => t.AverageScore).ToArray();
                string[] trajMonths = trajectory.Select(t => t.MonthLabel).ToArray();

                TrajectorySeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = trajValues,
                        Name = "Average Conduct Score",
                        Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(35)),
                        Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                        GeometrySize = 7,
                        GeometryFill = new SolidColorPaint(SKColors.DodgerBlue),
                        GeometryStroke = new SolidColorPaint(SKColors.White, 2)
                    }
                };

                TrajectoryXAxes = new Axis[]
                {
                    new Axis { Labels = trajMonths, LabelsRotation = 0 }
                };

                // Setup Bar Chart
                int[] catValues  = categories.Select(c => c.IncidentCount).ToArray();
                string[] catNames= categories.Select(c => c.CategoryName).ToArray();

                CategorySeries = new ISeries[]
                {
                    new ColumnSeries<int>
                    {
                        Values = catValues,
                        Name = "Incident Frequency",
                        Fill = new SolidColorPaint(SKColors.SteelBlue),
                        MaxBarWidth = 40
                    }
                };

                CategoryXAxes = new Axis[]
                {
                    new Axis { Labels = catNames, LabelsRotation = 15 }
                };

                WatchlistStudents = new ObservableCollection<WatchlistStudentModel>(watchlist);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading analytics dashboard:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}