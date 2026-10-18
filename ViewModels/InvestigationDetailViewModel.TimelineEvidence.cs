using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class InvestigationDetailViewModel
    {
        public ObservableCollection<InvestigationTimelineEntry> TimelineEntries { get; } = new();
        public ObservableCollection<IncidentEvidenceEntry> AvailableEvidenceList { get; } = new();
        public ObservableCollection<string> TimelineTypeOptions { get; } = new()
        {
            "Investigation Note",
            "Interview Log",
            "Witness Statement",
            "Evidence Review",
            "Evidence Observation",
            "Follow-Up Flag",
            "Status Update",
            "Intervention Action"
        };

        private string _newTimelineTitle = string.Empty;
        private string _newTimelineNotes = string.Empty;
        private string _newTimelineType = "Investigation Note";
        private IncidentEvidenceEntry? _selectedEvidence;

        public string NewTimelineTitle
        {
            get => _newTimelineTitle;
            set => SetProperty(ref _newTimelineTitle, value);
        }

        public string NewTimelineNotes
        {
            get => _newTimelineNotes;
            set => SetProperty(ref _newTimelineNotes, value);
        }

        public string NewTimelineType
        {
            get => _newTimelineType;
            set => SetProperty(ref _newTimelineType, value);
        }

        public IncidentEvidenceEntry? SelectedEvidence
        {
            get => _selectedEvidence;
            set => SetProperty(ref _selectedEvidence, value);
        }

        public bool HasEvidence => AvailableEvidenceList.Count > 0;
        public bool HasTimelineEntries => TimelineEntries.Count > 0;

        public ICommand StartInvestigationCommand { get; private set; } = null!;
        public ICommand AddTimelineEntryCommand { get; private set; } = null!;
        public ICommand OpenEvidenceCommand { get; private set; } = null!;
        public ICommand RecordInterviewLogCommand { get; private set; } = null!;
        public ICommand RecordWitnessStatementCommand { get; private set; } = null!;

        private void InitializeTimelineAndEvidence()
        {
            StartInvestigationCommand = new RelayCommand(async () => await StartInvestigationAsync());
            AddTimelineEntryCommand = new RelayCommand(async () => await AddTimelineEntryAsync());
            OpenEvidenceCommand = new RelayCommand(param => OpenEvidence(param as IncidentEvidenceEntry ?? SelectedEvidence));
            RecordInterviewLogCommand = new RelayCommand(async () => await RecordInterviewLogAsync());
            RecordWitnessStatementCommand = new RelayCommand(async () => await RecordWitnessStatementAsync());

            LoadAvailableEvidence();
            LoadTimelineEntries();
        }

        private void LoadAvailableEvidence()
        {
            AvailableEvidenceList.Clear();
            if (!string.IsNullOrWhiteSpace(_incident.EvidenceData))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<IncidentEvidenceEntry>>(_incident.EvidenceData);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            AvailableEvidenceList.Add(item);
                        }
                    }
                }
                catch { }
            }
            OnPropertyChanged(nameof(HasEvidence));
        }

        public void LoadTimelineEntries()
        {
            TimelineEntries.Clear();
            if (!string.IsNullOrWhiteSpace(_investigation.InvestigationTimeline))
            {
                try
                {
                    var entries = JsonSerializer.Deserialize<List<InvestigationTimelineEntry>>(_investigation.InvestigationTimeline);
                    if (entries != null)
                    {
                        foreach (var e in entries.OrderByDescending(x => x.Timestamp))
                        {
                            TimelineEntries.Add(e);
                        }
                    }
                }
                catch { }
            }

            // If empty, populate initial entry from investigation StartDate
            if (TimelineEntries.Count == 0)
            {
                TimelineEntries.Add(new InvestigationTimelineEntry
                {
                    ActionTitle = "Investigation Initiated",
                    Timestamp = _investigation.StartDate,
                    PerformedBy = string.IsNullOrWhiteSpace(_investigation.LeadInvestigatorName) ? "Intake Officer" : _investigation.LeadInvestigatorName,
                    Notes = "Case accepted and assigned for formal disciplinary inquiry.",
                    EntryType = "Status Update"
                });
            }

            OnPropertyChanged(nameof(HasTimelineEntries));
        }

        private async Task StartInvestigationAsync()
        {
            if (string.IsNullOrWhiteSpace(LeadInvestigatorName))
            {
                StatusMessage = "âš  Please assign a Lead Investigator before starting the investigation.";
                return;
            }

            try
            {
                _investigation.LeadInvestigatorName = LeadInvestigatorName.Trim();
                if (!TargetCompletionDate.HasValue)
                {
                    TargetCompletionDate = DateTime.Now.AddDays(7);
                }
                _investigation.TargetCompletionDate = TargetCompletionDate;

                // Step 5: Change incident status to Under Investigation
                await ChangeIncidentStatusAsync(IncidentStatus.UnderInvestigation);

                // Add to timeline
                var entry = new InvestigationTimelineEntry
                {
                    ActionTitle = "Investigation Officially Commenced",
                    Timestamp = DateTime.Now,
                    PerformedBy = LeadInvestigatorName.Trim(),
                    Notes = $"Assigned to {LeadInvestigatorName.Trim()} with target completion date {TargetCompletionDate:yyyy-MM-dd}.",
                    EntryType = "Status Update"
                };

                TimelineEntries.Insert(0, entry);
                await SaveTimelineEntriesAsync();
                await _investigationService.SaveInvestigationAsync(_investigation);

                StatusMessage = "âœ” Investigation started. Status changed to Under Investigation.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting investigation: {ex.Message}";
            }
        }

        private async Task AddTimelineEntryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTimelineTitle))
            {
                StatusMessage = "âš  Please enter an action title for the timeline entry.";
                return;
            }

            try
            {
                string performer = !string.IsNullOrWhiteSpace(LeadInvestigatorName)
                    ? LeadInvestigatorName.Trim()
                    : (SessionManager.CurrentUsername ?? "Investigator");

                var entry = new InvestigationTimelineEntry
                {
                    ActionTitle = NewTimelineTitle.Trim(),
                    Timestamp = DateTime.Now,
                    PerformedBy = performer,
                    Notes = NewTimelineNotes?.Trim() ?? string.Empty,
                    EntryType = NewTimelineType
                };

                TimelineEntries.Insert(0, entry);
                await SaveTimelineEntriesAsync();

                NewTimelineTitle = string.Empty;
                NewTimelineNotes = string.Empty;
                StatusMessage = "âœ” Timeline updated successfully.";
                OnPropertyChanged(nameof(HasTimelineEntries));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding timeline entry: {ex.Message}";
            }
        }

        private async Task RecordInterviewLogAsync()
        {
            if (string.IsNullOrWhiteSpace(InterviewLogs))
            {
                StatusMessage = "âš  No interview notes entered to log to timeline.";
                return;
            }

            var entry = new InvestigationTimelineEntry
            {
                ActionTitle = "Interview Statement Logged",
                Timestamp = DateTime.Now,
                PerformedBy = !string.IsNullOrWhiteSpace(LeadInvestigatorName) ? LeadInvestigatorName : (SessionManager.CurrentUsername ?? "Investigator"),
                Notes = InterviewLogs.Trim(),
                EntryType = "Interview Log"
            };

            TimelineEntries.Insert(0, entry);
            await SaveTimelineEntriesAsync();
            await SaveInvestigationAsync();
            StatusMessage = "âœ” Interview log synchronized to timeline.";
        }

        private async Task RecordWitnessStatementAsync()
        {
            if (string.IsNullOrWhiteSpace(WitnessStatements))
            {
                StatusMessage = "âš  No witness statement entered to log to timeline.";
                return;
            }

            var entry = new InvestigationTimelineEntry
            {
                ActionTitle = "Witness Testimony Documented",
                Timestamp = DateTime.Now,
                PerformedBy = !string.IsNullOrWhiteSpace(LeadInvestigatorName) ? LeadInvestigatorName : (SessionManager.CurrentUsername ?? "Investigator"),
                Notes = WitnessStatements.Trim(),
                EntryType = "Witness Statement"
            };

            TimelineEntries.Insert(0, entry);
            await SaveTimelineEntriesAsync();
            await SaveInvestigationAsync();
            StatusMessage = "âœ” Witness testimony synchronized to timeline.";
        }

        private async Task SaveTimelineEntriesAsync()
        {
            _investigation.InvestigationTimeline = JsonSerializer.Serialize(TimelineEntries.ToList());
            await _investigationService.SaveInvestigationAsync(_investigation);
        }

        private void OpenEvidence(IncidentEvidenceEntry? evidence)
        {
            if (evidence == null) return;

            try
            {
                if (evidence.IsLink && !string.IsNullOrWhiteSpace(evidence.FilePathOrUrl))
                {
                    Process.Start(new ProcessStartInfo(evidence.FilePathOrUrl) { UseShellExecute = true });
                    return;
                }

                if (!string.IsNullOrWhiteSpace(evidence.FilePathOrUrl) && File.Exists(evidence.FilePathOrUrl))
                {
                    Process.Start(new ProcessStartInfo(evidence.FilePathOrUrl) { UseShellExecute = true });
                }
                else
                {
                    StatusMessage = $"âš  Evidence file not found locally: {evidence.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening evidence: {ex.Message}";
            }
        }
    }
}