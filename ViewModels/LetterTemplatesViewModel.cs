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

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class LetterTemplatesViewModel : ObservableObject
    {
        private readonly ILetterTemplateService _templateService;
        private readonly IIncidentService _incidentService;
        private readonly IPdfExportService _pdfService;
        private readonly Action _onBack;

        private ObservableCollection<LetterTemplate> _availableTemplates = new();
        private LetterTemplate? _selectedTemplate;

        private ObservableCollection<Incident> _availableIncidents = new();
        private ObservableCollection<Incident> _filteredIncidents = new();
        private Incident? _selectedIncident;
        private string _incidentSearchText = string.Empty;

        private DateTime _actionDate = DateTime.Now.AddDays(3);
        private string _officerName = "David Taylor (Guidance Counselor)";
        private string _livePreviewText = string.Empty;
        private bool _isLoading;

        // Manage Templates Modal
        private bool _isManageModalOpen;
        private LetterTemplate? _editingTemplate;
        private string _templateNameInput = string.Empty;
        private string _templateBodyInput = string.Empty;

        public LetterTemplatesViewModel(
            ILetterTemplateService templateService,
            IIncidentService incidentService,
            IPdfExportService pdfService,
            Action onBack)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _pdfService      = pdfService      ?? throw new ArgumentNullException(nameof(pdfService));
            _onBack          = onBack          ?? throw new ArgumentNullException(nameof(onBack));

            BackToHubCommand             = new RelayCommand(_ => _onBack());
            ClearIncidentSearchCommand   = new RelayCommand(_ => IncidentSearchText = string.Empty);
            OpenTemplateManagerCommand   = new RelayCommand(_ => OpenTemplateManager());
            CloseTemplateManagerCommand  = new RelayCommand(_ => IsManageModalOpen = false);
            SaveTemplateCommand          = new RelayCommand(async _ => await SaveTemplateAsync());
            GeneratePdfCommand           = new RelayCommand(_ => GeneratePdf());

            _ = InitializeDataAsync();
        }

        public ObservableCollection<LetterTemplate> AvailableTemplates { get => _availableTemplates; set => SetProperty(ref _availableTemplates, value); }
        
        public LetterTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (SetProperty(ref _selectedTemplate, value))
                {
                    UpdateLivePreview();
                }
            }
        }

        public ObservableCollection<Incident> AvailableIncidents { get => _availableIncidents; set => SetProperty(ref _availableIncidents, value); }
        public ObservableCollection<Incident> FilteredIncidents  { get => _filteredIncidents;  set => SetProperty(ref _filteredIncidents, value); }

        public string IncidentSearchText
        {
            get => _incidentSearchText;
            set
            {
                if (SetProperty(ref _incidentSearchText, value))
                {
                    OnPropertyChanged(nameof(HasIncidentSearchText));
                    FilterIncidents();
                }
            }
        }

        public bool HasIncidentSearchText => !string.IsNullOrWhiteSpace(IncidentSearchText);

        public Incident? SelectedIncident
        {
            get => _selectedIncident;
            set
            {
                if (SetProperty(ref _selectedIncident, value))
                {
                    UpdateLivePreview();
                }
            }
        }

        public DateTime ActionDate
        {
            get => _actionDate;
            set
            {
                if (SetProperty(ref _actionDate, value))
                {
                    UpdateLivePreview();
                }
            }
        }

        public string OfficerName
        {
            get => _officerName;
            set
            {
                if (SetProperty(ref _officerName, value))
                {
                    UpdateLivePreview();
                }
            }
        }

        public string LivePreviewText { get => _livePreviewText; set => SetProperty(ref _livePreviewText, value); }
        public bool IsLoading          { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool IsManageModalOpen { get => _isManageModalOpen; set => SetProperty(ref _isManageModalOpen, value); }

        public LetterTemplate? EditingTemplate
        {
            get => _editingTemplate;
            set
            {
                if (SetProperty(ref _editingTemplate, value) && value != null)
                {
                    TemplateNameInput = value.TemplateName;
                    TemplateBodyInput = value.TemplateBody;
                }
            }
        }

        public string TemplateNameInput { get => _templateNameInput; set => SetProperty(ref _templateNameInput, value); }
        public string TemplateBodyInput { get => _templateBodyInput; set => SetProperty(ref _templateBodyInput, value); }

        // Commands
        public ICommand BackToHubCommand            { get; }
        public ICommand ClearIncidentSearchCommand   { get; }
        public ICommand OpenTemplateManagerCommand  { get; }
        public ICommand CloseTemplateManagerCommand { get; }
        public ICommand SaveTemplateCommand         { get; }
        public ICommand GeneratePdfCommand          { get; }

        public async Task InitializeDataAsync()
        {
            IsLoading = true;
            try
            {
                var templates = await _templateService.GetAllTemplatesAsync();
                AvailableTemplates = new ObservableCollection<LetterTemplate>(templates);

                var incidents = await _incidentService.GetAllIncidentsAsync();
                AvailableIncidents = new ObservableCollection<Incident>(incidents);
                FilterIncidents();

                if (AvailableTemplates.Any())
                {
                    SelectedTemplate = AvailableTemplates.First();
                }

                if (FilteredIncidents.Any())
                {
                    SelectedIncident = FilteredIncidents.First();
                }

                UpdateLivePreview();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterIncidents()
        {
            if (string.IsNullOrWhiteSpace(IncidentSearchText))
            {
                FilteredIncidents = new ObservableCollection<Incident>(AvailableIncidents);
            }
            else
            {
                string query = IncidentSearchText.Trim().ToLower();
                var matches = AvailableIncidents.Where(i =>
                    (!string.IsNullOrEmpty(i.IncidentNumber) && i.IncidentNumber.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(i.Title) && i.Title.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(i.StudentDisplayName) && i.StudentDisplayName.ToLower().Contains(query)) ||
                    (i.Student != null && !string.IsNullOrEmpty(i.Student.StudentNumber) && i.Student.StudentNumber.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(i.Category) && i.Category.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(i.Location) && i.Location.ToLower().Contains(query))
                ).ToList();

                FilteredIncidents = new ObservableCollection<Incident>(matches);
            }

            if (SelectedIncident == null || !FilteredIncidents.Contains(SelectedIncident))
            {
                if (FilteredIncidents.Any())
                {
                    SelectedIncident = FilteredIncidents.First();
                }
            }
        }

        public void UpdateLivePreview()
        {
            if (SelectedTemplate == null)
            {
                LivePreviewText = "Select a template type to preview letter content.";
                return;
            }

            string text = SelectedTemplate.TemplateBody;
            string studentName  = SelectedIncident?.StudentDisplayName ?? "Juan Dela Cruz";
            string studentId    = SelectedIncident?.Student?.StudentNumber ?? "STD-1042";
            string gradeSection = SelectedIncident?.Student != null 
                ? $"{SelectedIncident.Student.GradeLevel} - {SelectedIncident.Student.Section}" 
                : "Grade 10 - St. Thomas";

            string incidentDate = SelectedIncident?.IncidentDate.ToString("MMMM dd, yyyy") ?? DateTime.Now.ToString("MMMM dd, yyyy");
            string infraction   = SelectedIncident?.Category ?? "Behavioral Misconduct";
            string location     = SelectedIncident?.Location ?? "School Grounds";
            string actionDateStr= ActionDate.ToString("MMMM dd, yyyy");

            text = text.Replace("{{StudentName}}", studentName)
                       .Replace("{{StudentId}}", studentId)
                       .Replace("{{GradeSection}}", gradeSection)
                       .Replace("{{IncidentDate}}", incidentDate)
                       .Replace("{{Infraction}}", infraction)
                       .Replace("{{Location}}", location)
                       .Replace("{{ActionDate}}", actionDateStr)
                       .Replace("{{OfficerName}}", string.IsNullOrWhiteSpace(OfficerName) ? "David Taylor (Guidance Counselor)" : OfficerName);

            LivePreviewText = text;
        }

        private void OpenTemplateManager()
        {
            if (AvailableTemplates.Any())
            {
                EditingTemplate = AvailableTemplates.First();
            }
            IsManageModalOpen = true;
        }

        private async Task SaveTemplateAsync()
        {
            if (string.IsNullOrWhiteSpace(TemplateNameInput))
            {
                MessageBox.Show("Please provide a template name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var tpl = EditingTemplate ?? new LetterTemplate();
            tpl.TemplateName = TemplateNameInput;
            tpl.TemplateBody = TemplateBodyInput;

            await _templateService.SaveTemplateAsync(tpl);
            IsManageModalOpen = false;
            await InitializeDataAsync();
        }

        private void GeneratePdf()
        {
            if (string.IsNullOrWhiteSpace(LivePreviewText)) return;

            try
            {
                string title = SelectedTemplate?.TemplateName ?? "Official Notice";
                string sanitizedName = string.Concat((SelectedIncident?.StudentDisplayName ?? "Student").Split(Path.GetInvalidFileNameChars()));
                var dialog = new SaveFileDialog
                {
                    FileName = $"{sanitizedName}_{title.Replace(" ", "_")}.pdf",
                    Filter   = "PDF Files (*.pdf)|*.pdf",
                    Title    = "Save Official Parent Notice PDF"
                };

                if (dialog.ShowDialog() == true)
                {
                    _pdfService.GenerateParentNoticePdf(dialog.FileName, LivePreviewText, OfficerName, title);
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating PDF notice:\n{ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}