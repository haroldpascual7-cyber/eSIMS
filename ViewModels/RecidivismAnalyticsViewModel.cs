using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
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
    public class RecidivismAnalyticsViewModel : ObservableObject
    {
        private readonly IRecidivismService _recidivismService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;
        private readonly Action<string> _onOpenTranscript;

        private RecidivismSummaryModel _summary = new();
        private ObservableCollection<ChronicOffenderWatchItem> _chronicWatchlist = new();
        private ChronicOffenderWatchItem? _selectedWatchItem;

        public RecidivismAnalyticsViewModel(
            IRecidivismService recidivismService,
            IPdfExportService pdfService,
            Action onBack,
            Action<string> onOpenTranscript)
        {
            _recidivismService = recidivismService ?? throw new ArgumentNullException(nameof(recidivismService));
            _pdfService        = pdfService        ?? throw new ArgumentNullException(nameof(pdfService));
            _onBack             = onBack             ?? throw new ArgumentNullException(nameof(onBack));
            _onOpenTranscript   = onOpenTranscript   ?? throw new ArgumentNullException(nameof(onOpenTranscript));

            BackToHubCommand             = new RelayCommand(_ => _onBack());
            OpenStudentTranscriptCommand = new RelayCommand(param => OpenStudentTranscript(param as ChronicOffenderWatchItem ?? SelectedWatchItem));
            ExportReportCommand          = new RelayCommand(_ => ExportReport());

            _ = InitializeDataAsync();
        }

        public RecidivismSummaryModel Summary { get => _summary; set => SetProperty(ref _summary, value); }
        public ObservableCollection<ChronicOffenderWatchItem> ChronicWatchlist { get => _chronicWatchlist; set => SetProperty(ref _chronicWatchlist, value); }

        public ChronicOffenderWatchItem? SelectedWatchItem
        {
            get => _selectedWatchItem;
            set => SetProperty(ref _selectedWatchItem, value);
        }

        // LiveCharts2 Series
        public IEnumerable<ISeries> OffenderDistributionSeries { get; private set; } = new List<ISeries>();
        public IEnumerable<ISeries> InterventionSuccessSeries { get; private set; } = new List<ISeries>();
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

        // Commands
        public ICommand BackToHubCommand             { get; }
        public ICommand OpenStudentTranscriptCommand { get; }
        public ICommand ExportReportCommand          { get; }

        public async Task InitializeDataAsync()
        {
            Summary = await _recidivismService.GetRecidivismSummaryAsync();
            var list = await _recidivismService.GetChronicOffendersAsync();
            ChronicWatchlist = new ObservableCollection<ChronicOffenderWatchItem>(list);

            var metrics = await _recidivismService.GetInterventionSuccessMetricsAsync();

            // Setup Donut Chart (Offender Distribution)
            OffenderDistributionSeries = new ISeries[]
            {
                new PieSeries<double> { Values = new double[] { Summary.SingleOffenderCount }, Name = "Single Offense", InnerRadius = 45, Fill = new SolidColorPaint(new SKColor(5, 150, 105)) },
                new PieSeries<double> { Values = new double[] { Summary.RepeatOffenderCount }, Name = "Repeat Offense (2)", InnerRadius = 45, Fill = new SolidColorPaint(new SKColor(217, 119, 6)) },
                new PieSeries<double> { Values = new double[] { Summary.ChronicOffenderCount }, Name = "Chronic (3+)", InnerRadius = 45, Fill = new SolidColorPaint(new SKColor(220, 38, 38)) }
            };

            // Setup Column Chart (Intervention Success Rates)
            InterventionSuccessSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = metrics.Select(m => m.SuccessRatePercent).ToArray(),
                    Name = "Success Rate (%)",
                    Fill = new SolidColorPaint(new SKColor(37, 99, 235)),
                    MaxBarWidth = 32
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = metrics.Select(m => m.InterventionType).ToArray(),
                    LabelsRotation = 15,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(71, 85, 105))
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = 100,
                    Labeler = val => $"{val}%",
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(71, 85, 105))
                }
            };

            OnPropertyChanged(nameof(OffenderDistributionSeries));
            OnPropertyChanged(nameof(InterventionSuccessSeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        private void OpenStudentTranscript(ChronicOffenderWatchItem? item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.StudentId))
            {
                _onOpenTranscript(item.StudentId);
            }
        }

        private void ExportReport()
        {
            if (!ChronicWatchlist.Any())
            {
                MessageBox.Show("No data available to export.", "Export Report", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"Recidivism_Analytics_Report_{DateTime.Now:yyyy-MM-dd}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Recidivism & Offender Analytics Report PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateRecidivismReport(dialog.FileName, Summary, ChronicWatchlist.ToList());
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting recidivism report:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}