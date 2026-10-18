using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class InvestigationDetailViewModel
    {
        // -------------------------------------------------------------
        // FEATURE 1: Flag Missing or Conflicting Information for Follow-Up
        // -------------------------------------------------------------
        public ObservableCollection<string> FollowUpIssueOptions { get; } = new()
        {
            "Conflicting Witness Statements",
            "Missing Physical / Digital Evidence",
            "Unverified Alibi / Timeline Gap",
            "Unidentified Involved Person / Bystander",
            "Incomplete Incident Statement",
            "Disputed Account of Events",
            "Requires Secondary Follow-up Interview"
        };

        public bool IsFollowUpFlagged
        {
            get => _investigation.IsFollowUpFlagged;
            set
            {
                if (_investigation.IsFollowUpFlagged != value)
                {
                    _investigation.IsFollowUpFlagged = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasActiveFollowUpFlag));
                    OnPropertyChanged(nameof(FollowUpBadgeText));
                    OnPropertyChanged(nameof(FollowUpSummaryDisplay));
                }
            }
        }

        public string FollowUpIssueType
        {
            get => string.IsNullOrWhiteSpace(_investigation.FollowUpIssueType) ? "Conflicting Witness Statements" : _investigation.FollowUpIssueType;
            set
            {
                if (_investigation.FollowUpIssueType != value)
                {
                    _investigation.FollowUpIssueType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FollowUpBadgeText));
                    OnPropertyChanged(nameof(FollowUpSummaryDisplay));
                }
            }
        }

        public string FollowUpNotes
        {
            get => _investigation.FollowUpNotes;
            set
            {
                if (_investigation.FollowUpNotes != value)
                {
                    _investigation.FollowUpNotes = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FollowUpSummaryDisplay));
                }
            }
        }

        public bool IsFollowUpResolved
        {
            get => _investigation.IsFollowUpResolved;
            set
            {
                if (_investigation.IsFollowUpResolved != value)
                {
                    _investigation.IsFollowUpResolved = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasActiveFollowUpFlag));
                    OnPropertyChanged(nameof(FollowUpBadgeText));
                    OnPropertyChanged(nameof(FollowUpSummaryDisplay));
                }
            }
        }

        public bool HasActiveFollowUpFlag => IsFollowUpFlagged && !IsFollowUpResolved;

        public string FollowUpBadgeText
        {
            get
            {
                if (HasActiveFollowUpFlag)
                    return $"⚠️ Follow-Up Required: {FollowUpIssueType}";
                if (IsFollowUpFlagged && IsFollowUpResolved)
                    return "✓ Conflicting Information Reconciled & Resolved";
                return string.Empty;
            }
        }

        public string FollowUpSummaryDisplay
        {
            get
            {
                if (!IsFollowUpFlagged) return "No missing or conflicting information flagged.";
                string status = IsFollowUpResolved ? "[RECONCILED]" : "[PENDING FOLLOW-UP]";
                return $"{status} {FollowUpIssueType}: {FollowUpNotes}";
            }
        }

        public ICommand SaveFollowUpFlagCommand { get; private set; } = null!;
        public ICommand ResolveFollowUpFlagCommand { get; private set; } = null!;
        public ICommand ClearFollowUpFlagCommand { get; private set; } = null!;

        // -------------------------------------------------------------
        // FEATURE 2: Record Observations on Evidence (Without Altering Evidence)
        // -------------------------------------------------------------
        private IncidentEvidenceEntry? _selectedEvidenceForObservation;
        private bool _isObservationPanelOpen;
        private string _evidenceObservationNotes = string.Empty;

        public IncidentEvidenceEntry? SelectedEvidenceForObservation
        {
            get => _selectedEvidenceForObservation;
            set => SetProperty(ref _selectedEvidenceForObservation, value);
        }

        public bool IsObservationPanelOpen
        {
            get => _isObservationPanelOpen;
            set => SetProperty(ref _isObservationPanelOpen, value);
        }

        public string EvidenceObservationNotes
        {
            get => _evidenceObservationNotes;
            set => SetProperty(ref _evidenceObservationNotes, value);
        }

        public string OriginalEvidenceImmutableBadge => SelectedEvidenceForObservation != null
            ? $"🔒 Immutable Original Evidence: {SelectedEvidenceForObservation.Title} ({SelectedEvidenceForObservation.EvidenceType})"
            : "🔒 Original Evidence Intact";

        public ICommand StartEvidenceObservationCommand { get; private set; } = null!;
        public ICommand CancelEvidenceObservationCommand { get; private set; } = null!;
        public ICommand SaveEvidenceObservationCommand { get; private set; } = null!;

        public void InitializeObservationsAndFlags()
        {
            SaveFollowUpFlagCommand = new RelayCommand(async () => await SaveFollowUpFlagAsync());
            ResolveFollowUpFlagCommand = new RelayCommand(async () => await ResolveFollowUpFlagAsync());
            ClearFollowUpFlagCommand = new RelayCommand(async () => await ClearFollowUpFlagAsync());

            StartEvidenceObservationCommand = new RelayCommand(param =>
            {
                var ev = param as IncidentEvidenceEntry ?? SelectedEvidence;
                if (ev != null) StartEvidenceObservation(ev);
            });
            CancelEvidenceObservationCommand = new RelayCommand(() =>
            {
                IsObservationPanelOpen = false;
                EvidenceObservationNotes = string.Empty;
                SelectedEvidenceForObservation = null;
            });
            SaveEvidenceObservationCommand = new RelayCommand(async () => await SaveEvidenceObservationAsync());
        }

        private async Task SaveFollowUpFlagAsync()
        {
            if (string.IsNullOrWhiteSpace(FollowUpNotes))
            {
                StatusMessage = "⚠️ Please enter follow-up details explaining the missing or conflicting information.";
                return;
            }

            IsFollowUpFlagged = true;
            IsFollowUpResolved = false;

            string investigator = string.IsNullOrWhiteSpace(LeadInvestigatorName) ? "Lead Investigator" : LeadInvestigatorName;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
            string noteHeader = $"[Follow-Up Flag: {FollowUpIssueType} | {timestamp} by {investigator}]\n{FollowUpNotes.Trim()}";

            InvestigationNotes = string.IsNullOrWhiteSpace(InvestigationNotes)
                ? noteHeader
                : $"{InvestigationNotes}\n\n{noteHeader}";

            TimelineEntries.Insert(0, new InvestigationTimelineEntry
            {
                ActionTitle = $"Flagged: {FollowUpIssueType}",
                PerformedBy = investigator,
                Timestamp = DateTime.Now,
                Notes = $"Identified missing/conflicting information requiring follow-up: {FollowUpNotes.Trim()}",
                EntryType = "Status Update"
            });

            await SaveTimelineEntriesAsync();
            await SaveInvestigationAsync();
            StatusMessage = $"✓ Successfully flagged '{FollowUpIssueType}' for follow-up inquiry without altering evidence.";
        }

        private async Task ResolveFollowUpFlagAsync()
        {
            IsFollowUpResolved = true;
            string investigator = string.IsNullOrWhiteSpace(LeadInvestigatorName) ? "Lead Investigator" : LeadInvestigatorName;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
            string resolutionLog = $"[Follow-Up Resolved & Reconciled: {FollowUpIssueType} | {timestamp} by {investigator}]";

            InvestigationNotes = string.IsNullOrWhiteSpace(InvestigationNotes)
                ? resolutionLog
                : $"{InvestigationNotes}\n\n{resolutionLog}";

            TimelineEntries.Insert(0, new InvestigationTimelineEntry
            {
                ActionTitle = $"Reconciled: {FollowUpIssueType}",
                PerformedBy = investigator,
                Timestamp = DateTime.Now,
                Notes = "Follow-up completed and conflicting information resolved.",
                EntryType = "Status Update"
            });

            await SaveTimelineEntriesAsync();
            await SaveInvestigationAsync();
            StatusMessage = "✓ Flagged follow-up has been marked as reconciled and resolved.";
        }

        private async Task ClearFollowUpFlagAsync()
        {
            IsFollowUpFlagged = false;
            IsFollowUpResolved = false;
            FollowUpNotes = string.Empty;
            await SaveInvestigationAsync();
            StatusMessage = "Follow-up flag cleared.";
        }

        private void StartEvidenceObservation(IncidentEvidenceEntry evidence)
        {
            SelectedEvidenceForObservation = evidence;
            EvidenceObservationNotes = string.Empty;
            IsObservationPanelOpen = true;
            OnPropertyChanged(nameof(OriginalEvidenceImmutableBadge));
        }

        private async Task SaveEvidenceObservationAsync()
        {
            if (SelectedEvidenceForObservation == null)
            {
                StatusMessage = "⚠️ Please select an evidence item to observe.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EvidenceObservationNotes))
            {
                StatusMessage = "⚠️ Please enter your observation notes.";
                return;
            }

            string investigator = string.IsNullOrWhiteSpace(LeadInvestigatorName) ? "Investigator" : LeadInvestigatorName;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
            string observationEntry = $"[Evidence Observation - {SelectedEvidenceForObservation.Title} ({SelectedEvidenceForObservation.EvidenceType}) | {timestamp} by {investigator}]\n{EvidenceObservationNotes.Trim()}";

            // 1. Record observation in Investigation Notes without altering the original evidence
            InvestigationNotes = string.IsNullOrWhiteSpace(InvestigationNotes)
                ? observationEntry
                : $"{InvestigationNotes}\n\n{observationEntry}";

            // 2. Add to Chronological Timeline
            TimelineEntries.Insert(0, new InvestigationTimelineEntry
            {
                ActionTitle = $"Evidence Observation: {SelectedEvidenceForObservation.Title}",
                PerformedBy = investigator,
                Timestamp = DateTime.Now,
                Notes = EvidenceObservationNotes.Trim(),
                EntryType = "Evidence Review"
            });

            // 3. Save timeline and investigation (original evidence in _incident.EvidenceData remains 100% immutable)
            await SaveTimelineEntriesAsync();
            await SaveInvestigationAsync();

            StatusMessage = $"✓ Evidence observation for '{SelectedEvidenceForObservation.Title}' successfully recorded in investigation notes without altering original evidence.";
            IsObservationPanelOpen = false;
            EvidenceObservationNotes = string.Empty;
            SelectedEvidenceForObservation = null;
        }
    }
}
