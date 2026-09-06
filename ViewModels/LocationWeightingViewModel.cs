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
    public class LocationWeightingViewModel : ObservableObject
    {
        private readonly IIncidentClassificationService _classificationService;
        private readonly Action _onBackToRulesHub;

        private string _searchText = string.Empty;
        private string _selectedCategoryFilter = "All Categories";
        private string _selectedSupervisionFilter = "All Supervision";
        private string _statusMessage = string.Empty;

        // KPIs
        private int _totalCampusZonesCount;
        private int _blindSpotZonesCount;
        private int _highRiskMultipliersCount;
        private int _activeCctvAreasCount;

        // Modal State
        private bool _isModalOpen;
        private LocationWeightRule? _editingRule;
        private string _modalLocationName = string.Empty;
        private string _modalBuilding = string.Empty;
        private string _modalCategory = "Classroom";
        private string _modalSupervisionLevel = "High Constant Supervision";
        private bool _modalHasCCTV = true;
        private double _modalRiskMultiplier = 1.0;
        private bool _modalIsRestricted;
        private bool _modalIsActive = true;
        private string _modalErrorMessage = string.Empty;

        public LocationWeightingViewModel(
            IIncidentClassificationService classificationService,
            Action onBackToRulesHub)
        {
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _onBackToRulesHub = onBackToRulesHub ?? throw new ArgumentNullException(nameof(onBackToRulesHub));

            LocationRules = new ObservableCollection<LocationWeightRule>();

            CategoryFilterOptions = new ObservableCollection<string>
            {
                "All Categories",
                "Classroom",
                "Hallway/Stairwell",
                "Restroom/Locker",
                "Gym/Field",
                "Secluded/Perimeter"
            };

            SupervisionFilterOptions = new ObservableCollection<string>
            {
                "All Supervision",
                "High Constant Supervision",
                "Periodic Patrol",
                "Unmonitored Blind Spot"
            };

            CategoryOptions = new ObservableCollection<string>
            {
                "Classroom",
                "Hallway/Stairwell",
                "Restroom/Locker",
                "Gym/Field",
                "Secluded/Perimeter"
            };

            SupervisionLevelOptions = new ObservableCollection<string>
            {
                "High Constant Supervision",
                "Periodic Patrol",
                "Unmonitored Blind Spot"
            };

            BackToRulesHubCommand = new RelayCommand(() => _onBackToRulesHub());
            OpenAddModalCommand = new RelayCommand(OpenAddModal);
            OpenEditModalCommand = new RelayCommand(param => { if (param is LocationWeightRule r) OpenEditModal(r); });
            CloseModalCommand = new RelayCommand(CloseModal);
            SaveRuleCommand = new RelayCommand(async () => await SaveRuleAsync());
            DeleteRuleCommand = new RelayCommand(async param => { if (param is LocationWeightRule r) await DeleteRuleAsync(r); });

            _ = InitializeDataAsync();
        }

        public ObservableCollection<LocationWeightRule> LocationRules { get; }

        public ObservableCollection<string> CategoryFilterOptions { get; }
        public ObservableCollection<string> SupervisionFilterOptions { get; }
        public ObservableCollection<string> CategoryOptions { get; }
        public ObservableCollection<string> SupervisionLevelOptions { get; }

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

        public string SelectedSupervisionFilter
        {
            get => _selectedSupervisionFilter;
            set { if (SetProperty(ref _selectedSupervisionFilter, value)) _ = FilterRulesAsync(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // KPIs
        public int TotalCampusZonesCount { get => _totalCampusZonesCount; set => SetProperty(ref _totalCampusZonesCount, value); }
        public int BlindSpotZonesCount { get => _blindSpotZonesCount; set => SetProperty(ref _blindSpotZonesCount, value); }
        public int HighRiskMultipliersCount { get => _highRiskMultipliersCount; set => SetProperty(ref _highRiskMultipliersCount, value); }
        public int ActiveCctvAreasCount { get => _activeCctvAreasCount; set => SetProperty(ref _activeCctvAreasCount, value); }

        // Modal Properties
        public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }
        public string ModalLocationName { get => _modalLocationName; set => SetProperty(ref _modalLocationName, value); }
        public string ModalBuilding { get => _modalBuilding; set => SetProperty(ref _modalBuilding, value); }
        public string ModalCategory { get => _modalCategory; set => SetProperty(ref _modalCategory, value); }
        public string ModalSupervisionLevel
        {
            get => _modalSupervisionLevel;
            set
            {
                if (SetProperty(ref _modalSupervisionLevel, value))
                {
                    // Auto-adjust default multiplier based on supervision level
                    if (value.Contains("Blind")) ModalRiskMultiplier = 1.8;
                    else if (value.Contains("Periodic")) ModalRiskMultiplier = 1.3;
                    else ModalRiskMultiplier = 1.0;
                }
            }
        }
        public bool ModalHasCCTV { get => _modalHasCCTV; set => SetProperty(ref _modalHasCCTV, value); }
        public double ModalRiskMultiplier { get => _modalRiskMultiplier; set => SetProperty(ref _modalRiskMultiplier, value); }
        public bool ModalIsRestricted { get => _modalIsRestricted; set => SetProperty(ref _modalIsRestricted, value); }
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
            var rules = await _classificationService.GetAllLocationRulesAsync();
            UpdateRulesCollection(rules);
        }

        private async Task FilterRulesAsync()
        {
            var rules = await _classificationService.SearchAndFilterLocationRulesAsync(SearchText, SelectedCategoryFilter, SelectedSupervisionFilter);
            UpdateRulesCollection(rules);
        }

        private void UpdateRulesCollection(IEnumerable<LocationWeightRule> rules)
        {
            LocationRules.Clear();
            var list = rules.ToList();
            foreach (var r in list) LocationRules.Add(r);

            TotalCampusZonesCount = list.Count;
            BlindSpotZonesCount = list.Count(r => r.SupervisionLevel.Contains("Blind"));
            HighRiskMultipliersCount = list.Count(r => r.RiskMultiplier > 1.5);
            ActiveCctvAreasCount = list.Count(r => r.HasCCTV);
        }

        private void OpenAddModal()
        {
            _editingRule = null;
            ModalErrorMessage = string.Empty;
            ModalLocationName = string.Empty;
            ModalBuilding = string.Empty;
            ModalCategory = "Classroom";
            ModalSupervisionLevel = "High Constant Supervision";
            ModalHasCCTV = true;
            ModalRiskMultiplier = 1.0;
            ModalIsRestricted = false;
            ModalIsActive = true;

            IsModalOpen = true;
        }

        private void OpenEditModal(LocationWeightRule rule)
        {
            _editingRule = rule;
            ModalErrorMessage = string.Empty;
            ModalLocationName = rule.LocationName;
            ModalBuilding = rule.Building;
            ModalCategory = rule.Category;
            ModalSupervisionLevel = rule.SupervisionLevel;
            ModalHasCCTV = rule.HasCCTV;
            ModalRiskMultiplier = rule.RiskMultiplier;
            ModalIsRestricted = rule.IsRestricted;
            ModalIsActive = rule.IsActive;

            IsModalOpen = true;
        }

        private void CloseModal() => IsModalOpen = false;

        private async Task SaveRuleAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalLocationName))
            {
                ModalErrorMessage = "Please enter location / zone name.";
                return;
            }

            if (ModalRiskMultiplier < 1.0 || ModalRiskMultiplier > 2.5)
            {
                ModalErrorMessage = "Risk multiplier must be between 1.0x and 2.5x.";
                return;
            }

            if (_editingRule == null)
            {
                var newRule = new LocationWeightRule
                {
                    LocationCode = await _classificationService.GenerateLocationCodeAsync(),
                    LocationName = ModalLocationName.Trim(),
                    Building = ModalBuilding.Trim(),
                    Category = ModalCategory,
                    SupervisionLevel = ModalSupervisionLevel,
                    HasCCTV = ModalHasCCTV,
                    RiskMultiplier = Math.Round(ModalRiskMultiplier, 1),
                    IsRestricted = ModalIsRestricted,
                    IsActive = ModalIsActive
                };
                await _classificationService.AddLocationRuleAsync(newRule);
                StatusMessage = $"✔ Location rule '{newRule.LocationName}' created successfully.";
            }
            else
            {
                _editingRule.LocationName = ModalLocationName.Trim();
                _editingRule.Building = ModalBuilding.Trim();
                _editingRule.Category = ModalCategory;
                _editingRule.SupervisionLevel = ModalSupervisionLevel;
                _editingRule.HasCCTV = ModalHasCCTV;
                _editingRule.RiskMultiplier = Math.Round(ModalRiskMultiplier, 1);
                _editingRule.IsRestricted = ModalIsRestricted;
                _editingRule.IsActive = ModalIsActive;

                await _classificationService.UpdateLocationRuleAsync(_editingRule);
                StatusMessage = $"✔ Location rule '{_editingRule.LocationName}' updated.";
            }

            IsModalOpen = false;
            await LoadRulesAsync();
        }

        private async Task DeleteRuleAsync(LocationWeightRule rule)
        {
            await _classificationService.DeleteLocationRuleAsync(rule.Id);
            StatusMessage = $"✔ Location rule '{rule.LocationName}' deleted.";
            await LoadRulesAsync();
        }
    }
}