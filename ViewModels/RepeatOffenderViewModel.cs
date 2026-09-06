using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class RepeatOffenderViewModel : ObservableObject
    {
        private readonly IIncidentClassificationService _classificationService;
        private readonly Action _onBackToRulesHub;

        private string _searchText = string.Empty;
        private string _selectedCategoryFilter = "All Categories";
        private string _statusMessage = string.Empty;

        // KPIs
        private int _activePoliciesCount;
        private string _averageLookbackText = "30 Days";
        private int _triggeredEscalationsCount = 5;
        private int _flaggedOffendersCount = 3;

        // Modal State
        private bool _isModalOpen;
        private RepeatOffenderRule? _editingRule;
        private string _modalPolicyName = string.Empty;
        private string _modalCategory = "Cumulative Infractions";
        private int _modalLookbackDays = 30;
        private int _modalThresholdCount = 2;
        private string _modalPenaltyType = "Add Flat Risk Points";
        private int _modalBonusPoints = 25;
        private bool _modalAutoSummons = true;
        private bool _modalFlagProfile = true;
        private bool _modalRequireDeanSignoff = true;
        private bool _modalIsActive = true;
        private string _modalErrorMessage = string.Empty;

        public RepeatOffenderViewModel(
            IIncidentClassificationService classificationService,
            Action onBackToRulesHub)
        {
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _onBackToRulesHub = onBackToRulesHub ?? throw new ArgumentNullException(nameof(onBackToRulesHub));

            RepeatRules = new ObservableCollection<RepeatOffenderRule>();

            CategoryFilterOptions = new ObservableCollection<string>
            {
                "All Categories",
                "Cumulative Infractions",
                "Violence & Altercation",
                "Bullying & Harassment",
                "Vandalism & Property",
                "Substance & Contraband",
                "Attendance & Truancy"
            };

            CategoryOptions = new ObservableCollection<string>
            {
                "Cumulative Infractions",
                "Violence & Altercation",
                "Bullying & Harassment",
                "Vandalism & Property",
                "Substance & Contraband",
                "Attendance & Truancy"
            };

            LookbackOptions = new ObservableCollection<int> { 15, 30, 45, 60, 90 };

            PenaltyTypeOptions = new ObservableCollection<string>
            {
                "Add Flat Risk Points",
                "Bump Severity Tier by +1",
                "Force Critical Tier Immediately"
            };

            BackToRulesHubCommand = new RelayCommand(() => _onBackToRulesHub());
            OpenAddModalCommand = new RelayCommand(OpenAddModal);
            OpenEditModalCommand = new RelayCommand(param => { if (param is RepeatOffenderRule r) OpenEditModal(r); });
            CloseModalCommand = new RelayCommand(CloseModal);
            SaveRuleCommand = new RelayCommand(async () => await SaveRuleAsync());
            DeleteRuleCommand = new RelayCommand(async param => { if (param is RepeatOffenderRule r) await DeleteRuleAsync(r); });

            _ = InitializeDataAsync();
        }

        public ObservableCollection<RepeatOffenderRule> RepeatRules { get; }
        public ObservableCollection<string> CategoryFilterOptions { get; }
        public ObservableCollection<string> CategoryOptions { get; }
        public ObservableCollection<int> LookbackOptions { get; }
        public ObservableCollection<string> PenaltyTypeOptions { get; }

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _ = FilterRulesAsync(); }
        }

        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set { if (SetProperty(ref _selectedCategoryFilter, value)) _ = FilterRulesAsync(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // KPIs
        public int ActivePoliciesCount { get => _activePoliciesCount; set => SetProperty(ref _activePoliciesCount, value); }
        public string AverageLookbackText { get => _averageLookbackText; set => SetProperty(ref _averageLookbackText, value); }
        public int TriggeredEscalationsCount { get => _triggeredEscalationsCount; set => SetProperty(ref _triggeredEscalationsCount, value); }
        public int FlaggedOffendersCount { get => _flaggedOffendersCount; set => SetProperty(ref _flaggedOffendersCount, value); }

        // Modal Properties
        public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }
        public string ModalPolicyName { get => _modalPolicyName; set => SetProperty(ref _modalPolicyName, value); }
        public string ModalCategory { get => _modalCategory; set => SetProperty(ref _modalCategory, value); }
        public int ModalLookbackDays { get => _modalLookbackDays; set => SetProperty(ref _modalLookbackDays, value); }
        public int ModalThresholdCount { get => _modalThresholdCount; set => SetProperty(ref _modalThresholdCount, value); }
        public string ModalPenaltyType { get => _modalPenaltyType; set => SetProperty(ref _modalPenaltyType, value); }
        public int ModalBonusPoints { get => _modalBonusPoints; set => SetProperty(ref _modalBonusPoints, value); }
        public bool ModalAutoSummons { get => _modalAutoSummons; set => SetProperty(ref _modalAutoSummons, value); }
        public bool ModalFlagProfile { get => _modalFlagProfile; set => SetProperty(ref _modalFlagProfile, value); }
        public bool ModalRequireDeanSignoff { get => _modalRequireDeanSignoff; set => SetProperty(ref _modalRequireDeanSignoff, value); }
        public bool ModalIsActive { get => _modalIsActive; set => SetProperty(ref _modalIsActive, value); }
        public string ModalErrorMessage { get => _modalErrorMessage; set => SetProperty(ref _modalErrorMessage, value); }

        // Commands
        public ICommand BackToRulesHubCommand { get; }
        public ICommand OpenAddModalCommand { get; }
        public ICommand OpenEditModalCommand { get; }
        public ICommand CloseModalCommand { get; }
        public ICommand SaveRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }

        private async Task InitializeDataAsync()
        {
            await LoadRulesAsync();
        }

        private async Task LoadRulesAsync()
        {
            var rules = await _classificationService.GetAllRepeatOffenderRulesAsync();
            UpdateRulesCollection(rules);
        }

        private async Task FilterRulesAsync()
        {
            var rules = await _classificationService.SearchAndFilterRepeatOffenderRulesAsync(SearchText, SelectedCategoryFilter);
            UpdateRulesCollection(rules);
        }

        private void UpdateRulesCollection(IEnumerable<RepeatOffenderRule> rules)
        {
            RepeatRules.Clear();
            var list = rules.ToList();
            foreach (var r in list) RepeatRules.Add(r);

            ActivePoliciesCount = list.Count(r => r.IsActive);
            if (list.Any())
            {
                int avgDays = (int)Math.Round(list.Average(r => r.LookbackDays));
                AverageLookbackText = $"{avgDays} Days";
            }
            else
            {
                AverageLookbackText = "30 Days";
            }
        }

        private void OpenAddModal()
        {
            _editingRule = null;
            ModalErrorMessage = string.Empty;
            ModalPolicyName = string.Empty;
            ModalCategory = "Cumulative Infractions";
            ModalLookbackDays = 30;
            ModalThresholdCount = 2;
            ModalPenaltyType = "Add Flat Risk Points";
            ModalBonusPoints = 25;
            ModalAutoSummons = true;
            ModalFlagProfile = true;
            ModalRequireDeanSignoff = true;
            ModalIsActive = true;

            IsModalOpen = true;
        }

        private void OpenEditModal(RepeatOffenderRule rule)
        {
            _editingRule = rule;
            ModalErrorMessage = string.Empty;
            ModalPolicyName = rule.PolicyName;
            ModalCategory = rule.Category;
            ModalLookbackDays = rule.LookbackDays;
            ModalThresholdCount = rule.ThresholdCount;
            ModalPenaltyType = rule.PenaltyType;
            ModalBonusPoints = rule.BonusPoints;
            ModalAutoSummons = rule.AutoSummons;
            ModalFlagProfile = rule.FlagProfile;
            ModalRequireDeanSignoff = rule.RequireDeanSignoff;
            ModalIsActive = rule.IsActive;

            IsModalOpen = true;
        }

        private void CloseModal() => IsModalOpen = false;

        private async Task SaveRuleAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalPolicyName))
            {
                ModalErrorMessage = "Please enter policy title / name.";
                return;
            }

            if (ModalThresholdCount < 1 || ModalThresholdCount > 20)
            {
                ModalErrorMessage = "Threshold count must be at least 1.";
                return;
            }

            if (_editingRule == null)
            {
                var newRule = new RepeatOffenderRule
                {
                    PolicyId = await _classificationService.GeneratePolicyIdAsync(),
                    PolicyName = ModalPolicyName.Trim(),
                    Category = ModalCategory,
                    LookbackDays = ModalLookbackDays,
                    ThresholdCount = ModalThresholdCount,
                    PenaltyType = ModalPenaltyType,
                    BonusPoints = ModalBonusPoints,
                    AutoSummons = ModalAutoSummons,
                    FlagProfile = ModalFlagProfile,
                    RequireDeanSignoff = ModalRequireDeanSignoff,
                    IsActive = ModalIsActive
                };
                await _classificationService.AddRepeatOffenderRuleAsync(newRule);
                StatusMessage = $"✔ Recidivism policy '{newRule.PolicyName}' created successfully.";
            }
            else
            {
                _editingRule.PolicyName = ModalPolicyName.Trim();
                _editingRule.Category = ModalCategory;
                _editingRule.LookbackDays = ModalLookbackDays;
                _editingRule.ThresholdCount = ModalThresholdCount;
                _editingRule.PenaltyType = ModalPenaltyType;
                _editingRule.BonusPoints = ModalBonusPoints;
                _editingRule.AutoSummons = ModalAutoSummons;
                _editingRule.FlagProfile = ModalFlagProfile;
                _editingRule.RequireDeanSignoff = ModalRequireDeanSignoff;
                _editingRule.IsActive = ModalIsActive;

                await _classificationService.UpdateRepeatOffenderRuleAsync(_editingRule);
                StatusMessage = $"✔ Recidivism policy '{_editingRule.PolicyName}' updated.";
            }

            IsModalOpen = false;
            await LoadRulesAsync();
        }

        private async Task DeleteRuleAsync(RepeatOffenderRule rule)
        {
            await _classificationService.DeleteRepeatOffenderRuleAsync(rule.Id);
            StatusMessage = $"✔ Recidivism policy '{rule.PolicyName}' deleted.";
            await LoadRulesAsync();
        }
    }
}