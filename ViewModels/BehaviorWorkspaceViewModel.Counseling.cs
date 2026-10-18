using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class BehaviorWorkspaceViewModel
    {
        private IInvestigationService? _investigationServiceForCounseling;
        private ObservableCollection<DisciplinaryAction> _allCounselingReferrals = new();
        private ObservableCollection<DisciplinaryAction> _filteredCounselingReferrals = new();
        private string _counselingSearchText = string.Empty;
        private string _selectedCounselingFilterStatus = "All Statuses";
        private int _totalCounselingReferralsCount;
        private int _pendingCounselingCount;
        private int _completedCounselingCount;

        public ObservableCollection<DisciplinaryAction> FilteredCounselingReferrals => _filteredCounselingReferrals;

        public ObservableCollection<string> CounselingFilterStatusOptions { get; } = new()
        {
            "All Statuses", "Pending", "Completed"
        };

        public string CounselingSearchText
        {
            get => _counselingSearchText;
            set
            {
                if (SetProperty(ref _counselingSearchText, value))
                {
                    ApplyCounselingFilters();
                }
            }
        }

        public string SelectedCounselingFilterStatus
        {
            get => _selectedCounselingFilterStatus;
            set
            {
                if (SetProperty(ref _selectedCounselingFilterStatus, value))
                {
                    ApplyCounselingFilters();
                }
            }
        }

        public int TotalCounselingReferralsCount
        {
            get => _totalCounselingReferralsCount;
            set => SetProperty(ref _totalCounselingReferralsCount, value);
        }

        public int PendingCounselingCount
        {
            get => _pendingCounselingCount;
            set => SetProperty(ref _pendingCounselingCount, value);
        }

        public int CompletedCounselingCount
        {
            get => _completedCounselingCount;
            set => SetProperty(ref _completedCounselingCount, value);
        }

        public bool HasCounselingReferrals => _filteredCounselingReferrals.Count > 0;

        public ICommand RefreshCounselingReferralsCommand { get; private set; } = null!;
        public ICommand ConfirmCounselingCompleteCommand { get; private set; } = null!;
        public ICommand MarkIncidentResolvedCommand { get; private set; } = null!;

        private void InitializeCounselingWorkspace()
        {
            _investigationServiceForCounseling = new InvestigationService(new AppDbContext());

            RefreshCounselingReferralsCommand = new RelayCommand(async () => await LoadCounselingReferralsAsync());

            ConfirmCounselingCompleteCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction action)
                {
                    await ConfirmCounselingActionCompleteAsync(action);
                }
            });

            MarkIncidentResolvedCommand = new RelayCommand(async param =>
            {
                if (param is DisciplinaryAction action)
                {
                    await MarkIncidentResolvedFromActionAsync(action);
                }
            });

            _ = LoadCounselingReferralsAsync();
        }

        public async Task LoadCounselingReferralsAsync()
        {
            try
            {
                if (_investigationServiceForCounseling == null)
                    _investigationServiceForCounseling = new InvestigationService(new AppDbContext());

                var actions = await _investigationServiceForCounseling.GetAllDisciplinaryActionsAsync();

                _allCounselingReferrals.Clear();
                foreach (var act in actions)
                {
                    _allCounselingReferrals.Add(act);
                }

                ApplyCounselingFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Counseling] Error loading referrals: {ex.Message}");
            }
        }

        private void ApplyCounselingFilters()
        {
            var query = _allCounselingReferrals.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(CounselingSearchText))
            {
                var term = CounselingSearchText.Trim();
                query = query.Where(a =>
                    (a.Student != null && a.Student.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (a.Incident != null && a.Incident.IncidentNumber.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (a.ActionType != null && a.ActionType.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (a.ActionDetails != null && a.ActionDetails.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (SelectedCounselingFilterStatus == "Pending")
            {
                query = query.Where(a => a.Status != "Completed");
            }
            else if (SelectedCounselingFilterStatus == "Completed")
            {
                query = query.Where(a => a.Status == "Completed");
            }

            _filteredCounselingReferrals.Clear();
            foreach (var item in query)
            {
                _filteredCounselingReferrals.Add(item);
            }

            TotalCounselingReferralsCount = _allCounselingReferrals.Count;
            PendingCounselingCount = _allCounselingReferrals.Count(a => a.Status != "Completed");
            CompletedCounselingCount = _allCounselingReferrals.Count(a => a.Status == "Completed");

            OnPropertyChanged(nameof(FilteredCounselingReferrals));
            OnPropertyChanged(nameof(HasCounselingReferrals));
        }

        private async Task ConfirmCounselingActionCompleteAsync(DisciplinaryAction action)
        {
            try
            {
                if (_investigationServiceForCounseling == null)
                    _investigationServiceForCounseling = new InvestigationService(new AppDbContext());

                string counselorUser = !string.IsNullOrWhiteSpace(SessionManager.FullName) ? SessionManager.FullName : SessionManager.Username;
                string note = $"Counseling session completed by {counselorUser} on {DateTime.Now:yyyy-MM-dd HH:mm}.";

                await _investigationServiceForCounseling.CompleteDisciplinaryActionAndResolveIncidentAsync(action.Id, note, markIncidentResolved: false);
                action.Status = "Completed";
                action.CompletedDate = DateTime.Now;

                ApplyCounselingFilters();
                MessageBox.Show($"Counseling referral for {(action.Student?.FullName ?? "student")} has been marked as Completed.",
                    "Counseling Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error confirming counseling: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task MarkIncidentResolvedFromActionAsync(DisciplinaryAction action)
        {
            try
            {
                if (_investigationServiceForCounseling == null)
                    _investigationServiceForCounseling = new InvestigationService(new AppDbContext());

                var confirm = MessageBox.Show(
                    $"Are you sure you want to mark case '{action.Incident?.IncidentNumber}' as RESOLVED?\nThis confirms that guidance counseling and restorative interventions are fulfilled.",
                    "Confirm Incident Resolution",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    string counselorUser = !string.IsNullOrWhiteSpace(SessionManager.FullName) ? SessionManager.FullName : SessionManager.Username;
                    string note = $"Case resolved following guidance counseling by {counselorUser}.";

                    await _investigationServiceForCounseling.CompleteDisciplinaryActionAndResolveIncidentAsync(action.Id, note, markIncidentResolved: true);
                    action.Status = "Completed";
                    action.CompletedDate = DateTime.Now;

                    if (action.Incident != null)
                    {
                        action.Incident.Status = IncidentStatus.Resolved;
                    }

                    ApplyCounselingFilters();
                    MessageBox.Show($"Incident '{action.Incident?.IncidentNumber}' successfully marked as RESOLVED.",
                        "Incident Resolved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resolving incident: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
