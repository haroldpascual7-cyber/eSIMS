using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ProbationTrackingViewModel : ObservableObject
    {
        private readonly IProbationService _probationService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onBack;
        private ProbationRecord? _selectedRecord;
        private Incident? _formIncident;
        private Student? _formStudent;
        private Staff? _formAssignedStaff;
        private DateTime _formStartDate = DateTime.Today;
        private DateTime _formEndDate = DateTime.Today.AddDays(14);
        private string _formReason = string.Empty;
        private string _formConditions = string.Empty;
        private bool _isCreateOpen;
        private bool _isLoading;
        private string _formError = string.Empty;
        private string _statusMessage = string.Empty;
        private string _progressStatus = "On Track";
        private string _progressNotes = string.Empty;
        private DateTime _extensionEndDate = DateTime.Today.AddDays(21);
        private string _extensionReason = string.Empty;
        private string _outcomeSummary = string.Empty;

        public ProbationTrackingViewModel(
            IProbationService probationService,
            IIncidentService incidentService,
            Action onBack)
        {
            _probationService = probationService;
            _incidentService = incidentService;
            _onBack = onBack;
            Records = new ObservableCollection<ProbationRecord>();
            ProgressHistory = new ObservableCollection<ProbationProgressNote>();
            Incidents = new ObservableCollection<Incident>();
            Students = new ObservableCollection<Student>();
            StaffMembers = new ObservableCollection<Staff>();
            ProgressStatusOptions = new ObservableCollection<string>
            {
                "On Track", "Improving", "Needs Attention", "Condition Breach"
            };

            BackCommand = new RelayCommand(_onBack);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            CloseCreateCommand = new RelayCommand(() => IsCreateOpen = false);
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            SelectRecordCommand = new RelayCommand(p => { if (p is ProbationRecord r) SelectedRecord = r; });
            AddProgressCommand = new RelayCommand(async () => await AddProgressAsync());
            ExtendCommand = new RelayCommand(async () => await ExtendAsync());
            CompleteCommand = new RelayCommand(async () => await CompleteAsync());
            RefreshCommand = new RelayCommand(async () => await LoadRecordsAsync());
            _ = InitializeAsync();
        }

        public ObservableCollection<ProbationRecord> Records { get; }
        public ObservableCollection<ProbationProgressNote> ProgressHistory { get; }
        public ObservableCollection<Incident> Incidents { get; }
        public ObservableCollection<Student> Students { get; }
        public ObservableCollection<Staff> StaffMembers { get; }
        public ObservableCollection<string> ProgressStatusOptions { get; }

        public ProbationRecord? SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (!SetProperty(ref _selectedRecord, value)) return;
                ProgressHistory.Clear();
                if (value != null)
                {
                    foreach (var note in value.ProgressNotes.OrderByDescending(n => n.NoteDate))
                        ProgressHistory.Add(note);
                    ExtensionEndDate = value.EndDate.AddDays(7);
                }
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(CanUpdateSelected));
            }
        }

        public bool HasSelection => SelectedRecord != null;
        public bool CanUpdateSelected => SelectedRecord?.IsOpen == true;
        public int ActiveCount => Records.Count(r => r.IsOpen);
        public int CompletedCount => Records.Count(r => !r.IsOpen);
        public bool IsCreateOpen { get => _isCreateOpen; set => SetProperty(ref _isCreateOpen, value); }
        public string FormError { get => _formError; set => SetProperty(ref _formError, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public Incident? FormIncident
        {
            get => _formIncident;
            set
            {
                if (!SetProperty(ref _formIncident, value) || value?.Student == null) return;
                FormStudent = Students.FirstOrDefault(s => s.Id == value.Student.Id) ?? value.Student;
            }
        }
        public Student? FormStudent { get => _formStudent; set => SetProperty(ref _formStudent, value); }
        public Staff? FormAssignedStaff { get => _formAssignedStaff; set => SetProperty(ref _formAssignedStaff, value); }
        public DateTime FormStartDate { get => _formStartDate; set => SetProperty(ref _formStartDate, value); }
        public DateTime FormEndDate { get => _formEndDate; set => SetProperty(ref _formEndDate, value); }
        public string FormReason { get => _formReason; set => SetProperty(ref _formReason, value); }
        public string FormConditions { get => _formConditions; set => SetProperty(ref _formConditions, value); }
        public string ApprovedBy => SessionManager.CurrentUsername;
        public string ProgressStatus { get => _progressStatus; set => SetProperty(ref _progressStatus, value); }
        public string ProgressNotes { get => _progressNotes; set => SetProperty(ref _progressNotes, value); }
        public DateTime ExtensionEndDate { get => _extensionEndDate; set => SetProperty(ref _extensionEndDate, value); }
        public string ExtensionReason { get => _extensionReason; set => SetProperty(ref _extensionReason, value); }
        public string OutcomeSummary { get => _outcomeSummary; set => SetProperty(ref _outcomeSummary, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public ICommand BackCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand CloseCreateCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SelectRecordCommand { get; }
        public ICommand AddProgressCommand { get; }
        public ICommand ExtendCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                foreach (var incident in await _incidentService.GetAllIncidentsAsync()) Incidents.Add(incident);
                foreach (var student in await _incidentService.GetAllStudentsAsync()) Students.Add(student);
                foreach (var staff in await _incidentService.GetAllStaffAsync()) StaffMembers.Add(staff);
                await LoadRecordsAsync();
            }
            finally
            {
                await Task.Delay(100);
                IsLoading = false;
            }
        }

        private async Task LoadRecordsAsync(int? selectId = null)
        {
            var records = (await _probationService.GetAllAsync()).ToList();
            Records.Clear();
            foreach (var record in records) Records.Add(record);
            var nextSelection = records.FirstOrDefault(r => r.Id == selectId) ?? records.FirstOrDefault();
            SelectedRecord = null;
            SelectedRecord = nextSelection;
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(CompletedCount));
        }

        private void OpenCreate()
        {
            FormError = string.Empty;
            FormIncident = Incidents.FirstOrDefault();
            FormStudent ??= Students.FirstOrDefault();
            FormAssignedStaff = StaffMembers.FirstOrDefault();
            FormStartDate = DateTime.Today;
            FormEndDate = DateTime.Today.AddDays(14);
            FormReason = string.Empty;
            FormConditions = string.Empty;
            IsCreateOpen = true;
        }

        private async Task SaveAsync()
        {
            FormError = string.Empty;
            if (FormIncident == null || FormStudent == null || FormAssignedStaff == null)
            { FormError = "Select a verified incident, student, and responsible staff member."; return; }
            if (FormEndDate < FormStartDate)
            { FormError = "The monitoring end date cannot be before the start date."; return; }
            if (string.IsNullOrWhiteSpace(FormReason) || string.IsNullOrWhiteSpace(FormConditions))
            { FormError = "Reason and expected behavior conditions are required."; return; }

            try
            {
                var record = new ProbationRecord
                {
                    IncidentId = FormIncident.Id,
                    StudentId = FormStudent.Id,
                    StartDate = FormStartDate.Date,
                    EndDate = FormEndDate.Date,
                    Reason = FormReason.Trim(),
                    ExpectedBehaviorConditions = FormConditions.Trim(),
                    AssignedStaffId = FormAssignedStaff.Id
                };
                await _probationService.AddAsync(record);
                IsCreateOpen = false;
                StatusMessage = $"Probation {record.RecordNumber} was authorized and started.";
                await LoadRecordsAsync(record.Id);
            }
            catch (Exception ex) { FormError = ex.Message; }
        }

        private async Task AddProgressAsync()
        {
            if (SelectedRecord == null) return;
            try
            {
                await _probationService.AddProgressNoteAsync(SelectedRecord.Id, ProgressStatus, ProgressNotes);
                ProgressNotes = string.Empty;
                StatusMessage = "Progress note recorded.";
                await LoadRecordsAsync(SelectedRecord.Id);
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task ExtendAsync()
        {
            if (SelectedRecord == null) return;
            try
            {
                await _probationService.ExtendAsync(SelectedRecord.Id, ExtensionEndDate, ExtensionReason);
                ExtensionReason = string.Empty;
                StatusMessage = "Monitoring period extended with an audit note.";
                await LoadRecordsAsync(SelectedRecord.Id);
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task CompleteAsync()
        {
            if (SelectedRecord == null) return;
            try
            {
                await _probationService.CompleteAsync(SelectedRecord.Id, OutcomeSummary);
                OutcomeSummary = string.Empty;
                StatusMessage = "Probation monitoring completed and closed.";
                await LoadRecordsAsync(SelectedRecord.Id);
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }
    }
}
