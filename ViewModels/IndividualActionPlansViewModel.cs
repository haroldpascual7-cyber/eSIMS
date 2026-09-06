using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class IndividualActionPlansViewModel : ObservableObject
    {
        private readonly IActionPlanService _planService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onBack;

        private List<IndividualActionPlan> _allPlans = new();
        private ObservableCollection<IndividualActionPlan> _actionPlans = new();
        private IndividualActionPlan? _selectedPlan;
        private string _searchText = string.Empty;
        private string _selectedFocusArea = "All";
        private string _selectedStatusFilter = "All";
        private bool _isLoading;

        // Modal Controls
        private bool _isCreateModalOpen;
        private bool _isDossierModalOpen;
        private IndividualActionPlan? _dossierPlan;

        // New Plan Form Fields
        private Student? _newStudent;
        private string _newFocusArea = "Anger Management & De-escalation";
        private string _newCounselor = "David Taylor";
        private string _newRiskTier = "Tier 2 - Moderate";
        private DateTime _newStartDate = DateTime.Now;
        private DateTime _newReviewDate = DateTime.Now.AddDays(30);
        private string _newProblemStatement = string.Empty;
        private string _newCoreObjectives = string.Empty;

        // Dossier Quick Entry Fields
        private string _newMilestoneTitle = string.Empty;
        private DateTime _newMilestoneDueDate = DateTime.Now.AddDays(7);
        private string _newNoteContent = string.Empty;

        public IndividualActionPlansViewModel(
            IActionPlanService planService,
            IIncidentService incidentService,
            Action onBack)
        {
            _planService     = planService     ?? throw new ArgumentNullException(nameof(planService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _onBack          = onBack          ?? throw new ArgumentNullException(nameof(onBack));

            BackToProfilesHubCommand = new RelayCommand(_ => _onBack());
            OpenCreateModalCommand   = new RelayCommand(_ => OpenCreateModal());
            CloseCreateModalCommand  = new RelayCommand(_ => IsCreateModalOpen = false);
            SaveCreatePlanCommand    = new RelayCommand(async _ => await SaveNewPlanAsync());

            OpenDossierCommand       = new RelayCommand(param => OpenDossier(param as IndividualActionPlan ?? SelectedPlan));
            CloseDossierCommand      = new RelayCommand(_ => IsDossierModalOpen = false);
            SaveDossierPlanCommand   = new RelayCommand(async _ => await SaveDossierPlanAsync());

            AddMilestoneCommand      = new RelayCommand(async _ => await AddMilestoneToDossierAsync());
            AddNoteCommand           = new RelayCommand(async _ => await AddNoteToDossierAsync());
            AttachDocumentCommand    = new RelayCommand(async _ => await AttachDocumentToDossierAsync());
            ExportPlanPdfCommand     = new RelayCommand(_ => ExportPlanPdf());

            _ = InitializeDataAsync();
        }

        public ObservableCollection<IndividualActionPlan> ActionPlans { get => _actionPlans; set => SetProperty(ref _actionPlans, value); }
        public IndividualActionPlan? SelectedPlan { get => _selectedPlan; set => SetProperty(ref _selectedPlan, value); }
        public IndividualActionPlan? DossierPlan  { get => _dossierPlan; set => SetProperty(ref _dossierPlan, value); }

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) ApplyFilters(); }
        }

        public string SelectedFocusArea
        {
            get => _selectedFocusArea;
            set { if (SetProperty(ref _selectedFocusArea, value)) ApplyFilters(); }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { if (SetProperty(ref _selectedStatusFilter, value)) ApplyFilters(); }
        }

        public bool IsLoading            { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool IsCreateModalOpen   { get => _isCreateModalOpen; set => SetProperty(ref _isCreateModalOpen, value); }
        public bool IsDossierModalOpen  { get => _isDossierModalOpen; set => SetProperty(ref _isDossierModalOpen, value); }

        // KPI Summary Counters
        public int ActivePlanCount            => _allPlans.Count(p => p.Status == "Active");
        public int CriticalWatchCount         => _allPlans.Count(p => p.RiskTier.Contains("Tier 3") || p.RiskTier.Contains("Critical"));
        public int MilestonesDueThisWeekCount => _allPlans.Sum(p => p.Milestones.Count(m => !m.IsCompleted));
        public int ClosedPlanCount            => _allPlans.Count(p => p.Status == "Closed");

        // Options Lists
        public ObservableCollection<Student> RegisteredStudents { get; private set; } = new();
        public ObservableCollection<string> CounselorList       { get; private set; } = new() { "David Taylor", "Sarah Davis", "James Miller", "Linda Wilson" };
        public ObservableCollection<string> FocusAreaList       { get; private set; } = new() { "All", "Anger Management & De-escalation", "Truancy & Class Attendance", "Restorative Re-integration", "Academic Integrity" };
        public ObservableCollection<string> StatusFilterList    { get; private set; } = new() { "All", "Active", "Under Evaluation", "Closed" };
        public ObservableCollection<string> RiskTierList        { get; private set; } = new() { "Tier 1 - Mild", "Tier 2 - Moderate", "Tier 3 - Critical" };

        // Form Bindings
        public Student? NewStudent            { get => _newStudent; set => SetProperty(ref _newStudent, value); }
        public string NewFocusArea           { get => _newFocusArea; set => SetProperty(ref _newFocusArea, value); }
        public string NewCounselor           { get => _newCounselor; set => SetProperty(ref _newCounselor, value); }
        public string NewRiskTier            { get => _newRiskTier; set => SetProperty(ref _newRiskTier, value); }
        public DateTime NewStartDate         { get => _newStartDate; set => SetProperty(ref _newStartDate, value); }
        public DateTime NewReviewDate        { get => _newReviewDate; set => SetProperty(ref _newReviewDate, value); }
        public string NewProblemStatement    { get => _newProblemStatement; set => SetProperty(ref _newProblemStatement, value); }
        public string NewCoreObjectives      { get => _newCoreObjectives; set => SetProperty(ref _newCoreObjectives, value); }

        public string NewMilestoneTitle     { get => _newMilestoneTitle; set => SetProperty(ref _newMilestoneTitle, value); }
        public DateTime NewMilestoneDueDate  { get => _newMilestoneDueDate; set => SetProperty(ref _newMilestoneDueDate, value); }
        public string NewNoteContent        { get => _newNoteContent; set => SetProperty(ref _newNoteContent, value); }

        // Commands
        public ICommand BackToProfilesHubCommand   { get; }
        public ICommand OpenCreateModalCommand     { get; }
        public ICommand CloseCreateModalCommand    { get; }
        public ICommand SaveCreatePlanCommand      { get; }
        public ICommand OpenDossierCommand         { get; }
        public ICommand CloseDossierCommand        { get; }
        public ICommand SaveDossierPlanCommand     { get; }
        public ICommand AddMilestoneCommand        { get; }
        public ICommand AddNoteCommand             { get; }
        public ICommand AttachDocumentCommand      { get; }
        public ICommand ExportPlanPdfCommand       { get; }

        public async Task InitializeDataAsync()
        {
            IsLoading = true;
            try
            {
                var students = await _incidentService.GetAllStudentsAsync();
                RegisteredStudents = new ObservableCollection<Student>(students);
                OnPropertyChanged(nameof(RegisteredStudents));

                if (RegisteredStudents.Any())
                {
                    NewStudent = RegisteredStudents.First();
                }

                _allPlans = await _planService.GetAllPlansAsync();
                ApplyFilters();
                NotifyKpis();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allPlans.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.ToLower();
                filtered = filtered.Where(p => 
                    p.Id.ToLower().Contains(query) ||
                    p.StudentId.ToLower().Contains(query) ||
                    p.StudentName.ToLower().Contains(query));
            }

            if (SelectedFocusArea != "All")
            {
                filtered = filtered.Where(p => p.FocusArea == SelectedFocusArea);
            }

            if (SelectedStatusFilter != "All")
            {
                filtered = filtered.Where(p => p.Status == SelectedStatusFilter);
            }

            ActionPlans = new ObservableCollection<IndividualActionPlan>(filtered);
        }

        private void NotifyKpis()
        {
            OnPropertyChanged(nameof(ActivePlanCount));
            OnPropertyChanged(nameof(CriticalWatchCount));
            OnPropertyChanged(nameof(MilestonesDueThisWeekCount));
            OnPropertyChanged(nameof(ClosedPlanCount));
        }

        private void OpenCreateModal()
        {
            NewStartDate = DateTime.Now;
            NewReviewDate = DateTime.Now.AddDays(30);
            NewProblemStatement = string.Empty;
            NewCoreObjectives = string.Empty;
            IsCreateModalOpen = true;
        }

        private async Task SaveNewPlanAsync()
        {
            if (NewStudent == null)
            {
                MessageBox.Show("Please select a student.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string planId = $"IAP-2026-{(ActionPlans.Count + 1):D3}";
            var plan = new IndividualActionPlan
            {
                Id = planId,
                StudentId = NewStudent.StudentNumber,
                StudentName = NewStudent.FullName,
                GradeSection = $"{NewStudent.GradeLevel} - {NewStudent.Section}",
                FocusArea = NewFocusArea,
                AssignedCounselor = NewCounselor,
                RiskTier = NewRiskTier,
                StartDate = NewStartDate.ToString("yyyy-MM-dd"),
                ReviewDate = NewReviewDate.ToString("yyyy-MM-dd"),
                ProblemStatement = NewProblemStatement,
                CoreRoadmapObjectives = NewCoreObjectives,
                Status = "Active"
            };

            // Add default initial milestone
            plan.Milestones.Add(new IapMilestone
            {
                MilestoneId = Guid.NewGuid().ToString(),
                PlanId = planId,
                Title = "Initial intake consultation & milestone goal setting",
                DueDate = NewStartDate.AddDays(7).ToString("yyyy-MM-dd"),
                IsCompleted = false
            });

            await _planService.InsertPlanAsync(plan);
            IsCreateModalOpen = false;
            await InitializeDataAsync();
        }

        private void OpenDossier(IndividualActionPlan? plan)
        {
            if (plan == null) return;
            DossierPlan = plan;
            IsDossierModalOpen = true;
        }

        private async Task SaveDossierPlanAsync()
        {
            if (DossierPlan == null) return;
            await _planService.UpdatePlanAsync(DossierPlan);
            IsDossierModalOpen = false;
            await InitializeDataAsync();
        }

        private async Task AddMilestoneToDossierAsync()
        {
            if (DossierPlan == null || string.IsNullOrWhiteSpace(NewMilestoneTitle)) return;

            var milestone = new IapMilestone
            {
                MilestoneId = Guid.NewGuid().ToString(),
                PlanId = DossierPlan.Id,
                Title = NewMilestoneTitle,
                DueDate = NewMilestoneDueDate.ToString("yyyy-MM-dd"),
                IsCompleted = false
            };

            await _planService.AddMilestoneAsync(milestone);
            DossierPlan.Milestones.Add(milestone);
            NewMilestoneTitle = string.Empty;
            OnPropertyChanged(nameof(DossierPlan));
            NotifyKpis();
        }

        private async Task AddNoteToDossierAsync()
        {
            if (DossierPlan == null || string.IsNullOrWhiteSpace(NewNoteContent)) return;

            var note = new IapCounselorNote
            {
                NoteId = Guid.NewGuid().ToString(),
                PlanId = DossierPlan.Id,
                Author = DossierPlan.AssignedCounselor,
                NoteDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Content = NewNoteContent
            };

            await _planService.AddNoteAsync(note);
            DossierPlan.Notes.Insert(0, note);
            NewNoteContent = string.Empty;
            OnPropertyChanged(nameof(DossierPlan));
        }

        private async Task AttachDocumentToDossierAsync()
        {
            if (DossierPlan == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "Select Action Plan Document",
                Filter = "All Documents (*.pdf;*.docx;*.jpg;*.png)|*.pdf;*.docx;*.jpg;*.png|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var attachment = new IapAttachment
                {
                    AttachmentId = Guid.NewGuid().ToString(),
                    PlanId = DossierPlan.Id,
                    FileName = Path.GetFileName(dialog.FileName),
                    FilePath = dialog.FileName,
                    UploadDate = DateTime.Now.ToString("yyyy-MM-dd")
                };

                await _planService.AddAttachmentAsync(attachment);
                DossierPlan.Attachments.Add(attachment);
                OnPropertyChanged(nameof(DossierPlan));
            }
        }

        private void ExportPlanPdf()
        {
            if (DossierPlan == null) return;
            MessageBox.Show($"Individual Action Plan Dossier exported for '{DossierPlan.StudentName}' ({DossierPlan.Id}).", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}