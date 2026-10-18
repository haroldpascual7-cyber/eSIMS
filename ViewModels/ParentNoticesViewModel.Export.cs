using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class ParentNoticesViewModel
    {
        private void ExportNoticePdf(AcknowledgmentLog? log)
        {
            if (log == null) return;
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Notice_{log.NoticeId}.pdf");
                string body = string.IsNullOrWhiteSpace(log.Remarks)
                    ? $"Official Disciplinary Notice issued to {log.StudentName} regarding school behavioral policy."
                    : log.Remarks;
                _pdfService.GenerateParentNoticePdf(path, body, SessionManager.FullName, $"NOTICE REF: {log.NoticeId}");
                MessageBox.Show($"Official Notice PDF exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF Export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportOverdueReport()
        {
            try
            {
                var overdueList = _allNotices.Where(n => n.DisplayStatus == "Overdue").ToList();
                if (overdueList.Count == 0)
                {
                    MessageBox.Show("There are no overdue parent notices at this time.", "Compliance Report", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Overdue_Parent_Notices_{DateTime.Today:yyyyMMdd}.pdf");
                _pdfService.GenerateOverdueComplianceReport(path, overdueList);
                MessageBox.Show($"Overdue Compliance Report exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
