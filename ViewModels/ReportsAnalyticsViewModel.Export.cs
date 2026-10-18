using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class ReportsAnalyticsViewModel
    {
        private void ExportLocationReport()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Campus Safety Heatmap Report PDF",
                    Filter = "PDF Document (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    FileName = $"Campus_Safety_Heatmap_{DateTime.Today:yyyyMMdd}.pdf",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _pdfService.GenerateCampusSafetyHeatmapReport(saveDialog.FileName, _heatmapNodes.ToList());
                    MessageBox.Show($"Campus Safety Heatmap successfully saved to:\n{saveDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }
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
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Recidivism Watchlist Report PDF",
                    Filter = "PDF Document (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    FileName = $"Recidivism_Watchlist_{DateTime.Today:yyyyMMdd}.pdf",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _pdfService.GenerateRecidivismReport(saveDialog.FileName, RecidivismSummary, _chronicWatchlist.ToList());
                    MessageBox.Show($"Recidivism Report successfully saved to:\n{saveDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }
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
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Executive Safety Report PDF",
                    Filter = "PDF Document (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    FileName = $"Executive_Summary_{SelectedTimeframe.Replace(' ', '_')}.pdf",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _reportData.PeriodType = SelectedPeriod;
                    _reportData.TimeframeText = SelectedTimeframe;
                    _pdfService.GenerateExecutiveSummaryPdf(saveDialog.FileName, _reportData, _flags);
                    MessageBox.Show($"Executive Safety Report successfully saved to:\n{saveDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }
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
