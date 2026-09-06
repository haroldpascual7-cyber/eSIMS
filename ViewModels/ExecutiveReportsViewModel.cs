using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ExecutiveReportsViewModel : ObservableObject
    {
        private readonly IPdfExportService _pdfService;
        private readonly ICommunicationService _commService;
        private readonly Action _onBack;

        private string _selectedPeriod = "Monthly";
        private string _selectedTimeframe = "August 2026";
        private string _recipientEmail = "principal@smartacademy.edu.ph";
        private bool _isGenerating;

        private ExecutiveReportFlags _flags = new();
        private ExecutiveReportData _reportData = new();

        public ExecutiveReportsViewModel(
            IPdfExportService pdfService,
            ICommunicationService commService,
            Action onBack)
        {
            _pdfService  = pdfService  ?? throw new ArgumentNullException(nameof(pdfService));
            _commService = commService ?? throw new ArgumentNullException(nameof(commService));
            _onBack      = onBack      ?? throw new ArgumentNullException(nameof(onBack));

            BackToHubCommand        = new RelayCommand(_ => _onBack());
            ExportPdfOnlyCommand    = new RelayCommand(async _ => await GenerateReportAsync(sendEmail: false));
            GenerateAndEmailCommand = new RelayCommand(async _ => await GenerateReportAsync(sendEmail: true));

            PeriodOptions    = new List<string> { "Monthly", "Quarterly", "Annual" };
            TimeframeOptions = new List<string> { "August 2026", "July 2026", "Q3 2026", "Q2 2026", "Academic Year 2025-2026" };
        }

        public List<string> PeriodOptions    { get; }
        public List<string> TimeframeOptions { get; }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (SetProperty(ref _selectedPeriod, value))
                {
                    ReportData.PeriodType = value;
                    OnPropertyChanged(nameof(ReportData));
                }
            }
        }

        public string SelectedTimeframe
        {
            get => _selectedTimeframe;
            set
            {
                if (SetProperty(ref _selectedTimeframe, value))
                {
                    ReportData.TimeframeText = value;
                    OnPropertyChanged(nameof(ReportData));
                }
            }
        }

        public string RecipientEmail
        {
            get => _recipientEmail;
            set => SetProperty(ref _recipientEmail, value);
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set => SetProperty(ref _isGenerating, value);
        }

        public ExecutiveReportFlags Flags
        {
            get => _flags;
            set => SetProperty(ref _flags, value);
        }

        public ExecutiveReportData ReportData
        {
            get => _reportData;
            set => SetProperty(ref _reportData, value);
        }

        // Commands
        public ICommand BackToHubCommand        { get; }
        public ICommand ExportPdfOnlyCommand    { get; }
        public ICommand GenerateAndEmailCommand { get; }

        private async Task GenerateReportAsync(bool sendEmail)
        {
            IsGenerating = true;

            try
            {
                string targetPath = string.Empty;

                if (Flags.IsSavePdfLocal || !sendEmail)
                {
                    var dialog = new SaveFileDialog
                    {
                        FileName = $"Executive_Summary_Report_{SelectedTimeframe.Replace(" ", "_")}.pdf",
                        Filter   = "PDF Files (*.pdf)|*.pdf",
                        Title    = "Save Executive Board Summary Report PDF"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        targetPath = dialog.FileName;
                    }
                    else if (!sendEmail)
                    {
                        return; // User cancelled save dialog
                    }
                }

                if (string.IsNullOrEmpty(targetPath))
                {
                    string tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "Reports");
                    Directory.CreateDirectory(tempDir);
                    targetPath = Path.Combine(tempDir, $"Executive_Report_{Guid.NewGuid().ToString().Substring(0, 8)}.pdf");
                }

                // Generate QuestPDF document
                _pdfService.GenerateExecutiveSummaryPdf(targetPath, ReportData, Flags);

                // Handle email dispatch if requested
                if (sendEmail || Flags.IsEmailToPrincipal)
                {
                    await _commService.SendExecutiveSummaryEmailAsync(targetPath, RecipientEmail);
                    MessageBox.Show($"Executive Report successfully generated and emailed to {RecipientEmail}!", "Dispatch Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Executive Report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Open generated PDF
                Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating executive report:\n{ex.Message}", "Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }
    }
}