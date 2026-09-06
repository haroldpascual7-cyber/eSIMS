using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class IntegrityAuditViewModel : ObservableObject
    {
        private readonly IIntegrityAuditService _integrityService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private ObservableCollection<DiagnosticResultModel> _diagnosticResults = new();
        private double _scanProgress;
        private string _scanStatusText = "Ready to execute system security & data integrity diagnostic audit.";
        private bool _isScanning;

        public IntegrityAuditViewModel(
            IIntegrityAuditService integrityService,
            IPdfExportService pdfService,
            Action onBack)
        {
            _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
            _pdfService       = pdfService       ?? throw new ArgumentNullException(nameof(pdfService));
            _onBack            = onBack            ?? throw new ArgumentNullException(nameof(onBack));

            BackToHubCommand            = new RelayCommand(_ => _onBack());
            RunSystemAuditCommand       = new RelayCommand(async _ => await RunSystemAuditAsync(), _ => !IsScanning);
            RepairOrphansCommand        = new RelayCommand(async _ => await RepairOrphansAsync(), _ => !IsScanning);
            ExportComplianceCertCommand = new RelayCommand(_ => ExportComplianceCert());

            _ = RunSystemAuditAsync();
        }

        public ObservableCollection<DiagnosticResultModel> DiagnosticResults
        {
            get => _diagnosticResults;
            set => SetProperty(ref _diagnosticResults, value);
        }

        public double ScanProgress
        {
            get => _scanProgress;
            set => SetProperty(ref _scanProgress, value);
        }

        public string ScanStatusText
        {
            get => _scanStatusText;
            set => SetProperty(ref _scanStatusText, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        // Commands
        public ICommand BackToHubCommand            { get; }
        public ICommand RunSystemAuditCommand       { get; }
        public ICommand RepairOrphansCommand        { get; }
        public ICommand ExportComplianceCertCommand { get; }

        public async Task RunSystemAuditAsync()
        {
            IsScanning = true;
            ScanProgress = 0;
            DiagnosticResults.Clear();

            try
            {
                // Check 1: SQLite File B-Tree Integrity
                ScanStatusText = "Executing Check 1 of 3: PRAGMA integrity_check on SQLite database file...";
                ScanProgress = 15;
                await Task.Delay(400);

                var check1 = await _integrityService.CheckSqliteDatabaseIntegrityAsync();
                DiagnosticResults.Add(check1);
                ScanProgress = 33;

                // Check 2: Relational Foreign Key Orphan Scan
                ScanStatusText = "Executing Check 2 of 3: Scanning relational foreign keys for orphaned records...";
                ScanProgress = 50;
                await Task.Delay(400);

                var check2 = await _integrityService.CheckRelationalOrphansAsync();
                DiagnosticResults.Add(check2);
                ScanProgress = 66;

                // Check 3: Cryptographic SHA-256 Row Hash Immutability
                ScanStatusText = "Executing Check 3 of 3: Verifying SHA-256 cryptographic row hashes across IncidentLogs...";
                var check3 = await _integrityService.CheckCryptographicRowHashesAsync((current, total) =>
                {
                    double subProgress = 66 + ((double)current / total * 34.0);
                    ScanProgress = Math.Min(99, subProgress);
                    ScanStatusText = $"Verifying SHA-256 hashes: row {current} of {total}...";
                });
                DiagnosticResults.Add(check3);

                ScanProgress = 100;
                ScanStatusText = "System audit complete. All 3 security diagnostic checks finished.";

                _ = AuditLogger.LogAsync("ADMIN-01", "CREATE", "Audit Trail", "Executed full security & data integrity audit scan.");
            }
            catch (Exception ex)
            {
                ScanStatusText = $"Audit error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task RepairOrphansAsync()
        {
            IsScanning = true;
            try
            {
                ScanStatusText = "Repairing orphaned records in IndividualActionPlans...";
                int repaired = await _integrityService.RepairOrphanRecordsAsync();

                _ = AuditLogger.LogAsync("ADMIN-01", "DELETE", "Audit Trail", $"Repaired {repaired} orphaned records.");
                MessageBox.Show($"Successfully repaired {repaired} orphaned action plan records!", "Repair Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                await RunSystemAuditAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error repairing records:\n{ex.Message}", "Repair Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void ExportComplianceCert()
        {
            if (!DiagnosticResults.Any())
            {
                MessageBox.Show("No audit results available to export. Please run a system audit first.", "Export Certificate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"ComplianceCertificate_{DateTime.Now:yyyy-MM-dd}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Data Integrity Compliance Certificate PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateComplianceCertificatePdf(dialog.FileName, DiagnosticResults.ToList());

                    _ = AuditLogger.LogAsync("ADMIN-01", "EXPORT", "Audit Trail", "Exported Data Integrity & Compliance Certificate PDF.");

                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting compliance certificate:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}