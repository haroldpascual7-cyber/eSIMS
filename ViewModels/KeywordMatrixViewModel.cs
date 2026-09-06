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
    public class KeywordMatrixViewModel : ObservableObject
    {
        private readonly IIncidentClassificationService _classificationService;
        private readonly Action _onBackToRulesHub;

        private string _searchText = string.Empty;
        private string _selectedCategoryFilter = "All Categories";
        private string _selectedSeverityFilter = "All Severities";
        private string _statusMessage = string.Empty;

        // KPIs
        private int _totalKeywordsCount;
        private int _criticalHighTriggersCount;
        private int _autoEscalationTermsCount;
        private int _activeRulesCount;

        // Sandbox State
        private string _testNarrativeText = "A student was reported carrying a switchblade near the cafeteria. An altercation broke out involving physical assault.";
        private TextAnalysisResult? _testAnalysisResult;
        private string _flaggedKeywordsDisplay = "None";
        private bool _isAnalysisRun;

        // Modal State
        private bool _isModalOpen;
        private KeywordSensitivityRule? _editingRule;
        private string _modalKeyword = string.Empty;
        private string _modalCategory = "Violence & Altercation";
        private string _modalSeverity = "Medium";
        private int _modalWeightScore = 25;
        private string _modalMatchType = "Contains Word Root";
        private bool _modalAutoEscalate;
        private bool _modalIsActive = true;
        private string _modalErrorMessage = string.Empty;

        public KeywordMatrixViewModel(
            IIncidentClassificationService classificationService,
            Action onBackToRulesHub)
        {
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _onBackToRulesHub = onBackToRulesHub ?? throw new ArgumentNullException(nameof(onBackToRulesHub));

            KeywordRules = new ObservableCollection<KeywordSensitivityRule>();

            CategoryFilterOptions = new ObservableCollection<string>
            {
                "All Categories",
                "Violence & Altercation",
                "Weapons & Threats",
                "Substance & Contraband",
                "Bullying & Harassment",
                "Vandalism & Property",
                "Academic & Protocol"
            };

            SeverityFilterOptions = new ObservableCollection<string>
            {
                "All Severities",
                "Low",
                "Medium",
                "High",
                "Critical"
            };

            CategoryOptions = new ObservableCollection<string>
            {
                "Violence & Altercation",
                "Weapons & Threats",
                "Substance & Contraband",
                "Bullying & Harassment",
                "Vandalism & Property",
                "Academic & Protocol"
            };

            SeverityOptions = new ObservableCollection<string> { "Low", "Medium", "High", "Critical" };
            MatchTypeOptions = new ObservableCollection<string> { "Exact Match", "Contains Word Root", "Regex Pattern" };

            BackToRulesHubCommand = new RelayCommand(() => _onBackToRulesHub());
            OpenAddModalCommand = new RelayCommand(OpenAddModal);
            OpenEditModalCommand = new RelayCommand(param => { if (param is KeywordSensitivityRule r) OpenEditModal(r); });
            CloseModalCommand = new RelayCommand(CloseModal);
            SaveRuleCommand = new RelayCommand(async () => await SaveRuleAsync());
            DeleteRuleCommand = new RelayCommand(async param => { if (param is KeywordSensitivityRule r) await DeleteRuleAsync(r); });
            RunAnalysisCommand = new RelayCommand(async () => await RunAnalysisAsync());

            _ = InitializeDataAsync();
        }

        public ObservableCollection<KeywordSensitivityRule> KeywordRules { get; }

        public ObservableCollection<string> CategoryFilterOptions { get; }
        public ObservableCollection<string> SeverityFilterOptions { get; }
        public ObservableCollection<string> CategoryOptions { get; }
        public ObservableCollection<string> SeverityOptions { get; }
        public ObservableCollection<string> MatchTypeOptions { get; }

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

        public string SelectedSeverityFilter
        {
            get => _selectedSeverityFilter;
            set { if (SetProperty(ref _selectedSeverityFilter, value)) _ = FilterRulesAsync(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // KPIs
        public int TotalKeywordsCount { get => _totalKeywordsCount; set => SetProperty(ref _totalKeywordsCount, value); }
        public int CriticalHighTriggersCount { get => _criticalHighTriggersCount; set => SetProperty(ref _criticalHighTriggersCount, value); }
        public int AutoEscalationTermsCount { get => _autoEscalationTermsCount; set => SetProperty(ref _autoEscalationTermsCount, value); }
        public int ActiveRulesCount { get => _activeRulesCount; set => SetProperty(ref _activeRulesCount, value); }

        // Sandbox Properties
        public string TestNarrativeText
        {
            get => _testNarrativeText;
            set => SetProperty(ref _testNarrativeText, value);
        }

        public TextAnalysisResult? TestAnalysisResult
        {
            get => _testAnalysisResult;
            set => SetProperty(ref _testAnalysisResult, value);
        }

        public string FlaggedKeywordsDisplay
        {
            get => _flaggedKeywordsDisplay;
            set => SetProperty(ref _flaggedKeywordsDisplay, value);
        }

        public bool IsAnalysisRun
        {
            get => _isAnalysisRun;
            set => SetProperty(ref _isAnalysisRun, value);
        }

        // Modal Properties
        public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }
        public string ModalKeyword { get => _modalKeyword; set => SetProperty(ref _modalKeyword, value); }
        public string ModalCategory { get => _modalCategory; set => SetProperty(ref _modalCategory, value); }
        public string ModalSeverity
        {
            get => _modalSeverity;
            set
            {
                if (SetProperty(ref _modalSeverity, value))
                {
                    // Auto suggest default weight based on tier
                    if (value == "Critical") ModalWeightScore = 90;
                    else if (value == "High") ModalWeightScore = 70;
                    else if (value == "Medium") ModalWeightScore = 40;
                    else if (value == "Low") ModalWeightScore = 15;
                }
            }
        }
        public int ModalWeightScore { get => _modalWeightScore; set => SetProperty(ref _modalWeightScore, value); }
        public string ModalMatchType { get => _modalMatchType; set => SetProperty(ref _modalMatchType, value); }
        public bool ModalAutoEscalate { get => _modalAutoEscalate; set => SetProperty(ref _modalAutoEscalate, value); }
        public bool ModalIsActive { get => _modalIsActive; set => SetProperty(ref _modalIsActive, value); }
        public string ModalErrorMessage { get => _modalErrorMessage; set => SetProperty(ref _modalErrorMessage, value); }

        // Commands
        public ICommand BackToRulesHubCommand { get; }
        public ICommand OpenAddModalCommand { get; }
        public ICommand OpenEditModalCommand { get; }
        public ICommand CloseModalCommand { get; }
        public ICommand SaveRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }
        public ICommand RunAnalysisCommand { get; }

        private async Task InitializeDataAsync()
        {
            await LoadRulesAsync();
            await RunAnalysisAsync();
        }

        private async Task LoadRulesAsync()
        {
            var rules = await _classificationService.GetAllRulesAsync();
            UpdateRulesCollection(rules);
        }

        private async Task FilterRulesAsync()
        {
            var rules = await _classificationService.SearchAndFilterRulesAsync(SearchText, SelectedCategoryFilter, SelectedSeverityFilter);
            UpdateRulesCollection(rules);
        }

        private void UpdateRulesCollection(IEnumerable<KeywordSensitivityRule> rules)
        {
            KeywordRules.Clear();
            var list = rules.ToList();
            foreach (var r in list) KeywordRules.Add(r);

            TotalKeywordsCount = list.Count;
            CriticalHighTriggersCount = list.Count(r => r.Severity == "Critical" || r.Severity == "High");
            AutoEscalationTermsCount = list.Count(r => r.AutoEscalate);
            ActiveRulesCount = list.Count(r => r.IsActive);
        }

        private void OpenAddModal()
        {
            _editingRule = null;
            ModalErrorMessage = string.Empty;
            ModalKeyword = string.Empty;
            ModalCategory = "Violence & Altercation";
            ModalSeverity = "Medium";
            ModalWeightScore = 30;
            ModalMatchType = "Contains Word Root";
            ModalAutoEscalate = false;
            ModalIsActive = true;

            IsModalOpen = true;
        }

        private void OpenEditModal(KeywordSensitivityRule rule)
        {
            _editingRule = rule;
            ModalErrorMessage = string.Empty;
            ModalKeyword = rule.Keyword;
            ModalCategory = rule.Category;
            ModalSeverity = rule.Severity;
            ModalWeightScore = rule.WeightScore;
            ModalMatchType = rule.MatchType;
            ModalAutoEscalate = rule.AutoEscalate;
            ModalIsActive = rule.IsActive;

            IsModalOpen = true;
        }

        private void CloseModal() => IsModalOpen = false;

        private async Task SaveRuleAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalKeyword))
            {
                ModalErrorMessage = "Please enter a keyword or trigger phrase.";
                return;
            }

            if (ModalWeightScore < 1 || ModalWeightScore > 100)
            {
                ModalErrorMessage = "Weight score must be between 1 and 100.";
                return;
            }

            if (_editingRule == null)
            {
                var newRule = new KeywordSensitivityRule
                {
                    RuleId = await _classificationService.GenerateRuleIdAsync(),
                    Keyword = ModalKeyword.Trim().ToLower(),
                    Category = ModalCategory,
                    Severity = ModalSeverity,
                    WeightScore = ModalWeightScore,
                    MatchType = ModalMatchType,
                    AutoEscalate = ModalAutoEscalate,
                    IsActive = ModalIsActive
                };
                await _classificationService.AddRuleAsync(newRule);
                StatusMessage = $"✔ Keyword rule '{newRule.Keyword}' created successfully.";
            }
            else
            {
                _editingRule.Keyword = ModalKeyword.Trim().ToLower();
                _editingRule.Category = ModalCategory;
                _editingRule.Severity = ModalSeverity;
                _editingRule.WeightScore = ModalWeightScore;
                _editingRule.MatchType = ModalMatchType;
                _editingRule.AutoEscalate = ModalAutoEscalate;
                _editingRule.IsActive = ModalIsActive;

                await _classificationService.UpdateRuleAsync(_editingRule);
                StatusMessage = $"✔ Keyword rule '{_editingRule.Keyword}' updated.";
            }

            IsModalOpen = false;
            await LoadRulesAsync();
            await RunAnalysisAsync();
        }

        private async Task DeleteRuleAsync(KeywordSensitivityRule rule)
        {
            await _classificationService.DeleteRuleAsync(rule.Id);
            StatusMessage = $"✔ Keyword rule '{rule.Keyword}' deleted.";
            await LoadRulesAsync();
            await RunAnalysisAsync();
        }

        private async Task RunAnalysisAsync()
        {
            TestAnalysisResult = await _classificationService.AnalyzeIncidentTextAsync(TestNarrativeText);
            IsAnalysisRun = true;

            if (TestAnalysisResult.FlaggedKeywords.Any())
            {
                FlaggedKeywordsDisplay = string.Join(", ", TestAnalysisResult.FlaggedKeywords);
            }
            else
            {
                FlaggedKeywordsDisplay = "None detected";
            }
        }
    }
}