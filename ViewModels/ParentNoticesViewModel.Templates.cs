using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class ParentNoticesViewModel
    {
        private ObservableCollection<LetterTemplateItemViewModel> _enhancedTemplates = new();
        public ObservableCollection<LetterTemplateItemViewModel> EnhancedTemplates => _enhancedTemplates;

        private LetterTemplateItemViewModel? _viewingEnhancedTemplate;
        public LetterTemplateItemViewModel? ViewingEnhancedTemplate
        {
            get => _viewingEnhancedTemplate;
            set => SetProperty(ref _viewingEnhancedTemplate, value);
        }

        public string RealisticCurrentDate => DateTime.Today.ToString("MMMM dd, yyyy");

        public void PopulateEnhancedTemplates(List<LetterTemplate> tpls)
        {
            _templates.Clear();
            _enhancedTemplates.Clear();
            foreach (var t in tpls)
            {
                _templates.Add(t);
                _enhancedTemplates.Add(new LetterTemplateItemViewModel(t));
            }
        }

        public ICommand PreviewTemplateCommand => new RelayCommand<object>(param =>
        {
            if (param is LetterTemplateItemViewModel vm)
            {
                ViewingEnhancedTemplate = vm;
                ViewingTemplate = vm.Template;
                IsViewTemplateModalOpen = true;
            }
            else if (param is LetterTemplate t)
            {
                ViewingTemplate = t;
                ViewingEnhancedTemplate = EnhancedTemplates.FirstOrDefault(x => x.Id == t.Id) ?? new LetterTemplateItemViewModel(t);
                IsViewTemplateModalOpen = true;
            }
        });

        public ICommand UseTemplateInNoticeCommand => new RelayCommand<object>(param =>
        {
            LetterTemplate? targetTemplate = null;
            if (param is LetterTemplateItemViewModel vm) targetTemplate = vm.Template;
            else if (param is LetterTemplate t) targetTemplate = t;
            else if (ViewingTemplate != null) targetTemplate = ViewingTemplate;

            if (targetTemplate != null)
            {
                IsViewTemplateModalOpen = false;
                OpenCreateNotice();
                ModalSelectedTemplate = targetTemplate;
            }
        });

        public ICommand ExportTemplatePdfDirectCommand => new RelayCommand<object>(param =>
        {
            LetterTemplate? targetTemplate = null;
            if (param is LetterTemplateItemViewModel vm) targetTemplate = vm.Template;
            else if (param is LetterTemplate t) targetTemplate = t;
            else if (ViewingTemplate != null) targetTemplate = ViewingTemplate;

            if (targetTemplate == null) return;

            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string cleanName = targetTemplate.TemplateName.Replace(" ", "_").Replace("/", "_");
                string path = Path.Combine(desktop, $"Official_{cleanName}_{DateTime.Today:yyyyMMdd}.pdf");

                string sName = SelectedNoticeStudent?.FullName ?? "Juan Dela Cruz";
                string sId = SelectedNoticeStudent?.StudentNumber ?? "STD-1042";
                string gSec = SelectedNoticeStudent != null ? $"{SelectedNoticeStudent.GradeLevel} - {SelectedNoticeStudent.Section}" : "Grade 10 - Rizal";
                string dateStr = DateTime.Today.ToString("MMMM dd, yyyy");

                string body = targetTemplate.TemplateBody
                    .Replace("{{StudentName}}", sName)
                    .Replace("{{StudentId}}", sId)
                    .Replace("{{GradeSection}}", gSec)
                    .Replace("{{ActionDate}}", dateStr)
                    .Replace("{{IncidentDate}}", dateStr)
                    .Replace("{{Location}}", "Campus Grounds")
                    .Replace("{{Infraction}}", "Behavioral Conduct Infraction")
                    .Replace("{{OfficerName}}", SessionManager.FullName);

                _pdfService.GenerateParentNoticePdf(path, body, SessionManager.FullName, targetTemplate.TemplateName);

                var res = MessageBox.Show(
                    $"Official Letter PDF generated successfully!\n\nFile saved to:\n{path}\n\nWould you like to open the PDF document now?",
                    "Export Successful",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }
}
