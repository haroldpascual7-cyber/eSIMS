using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IIncidentClassificationService
    {
        // Keyword Rules
        Task<IEnumerable<KeywordSensitivityRule>> GetAllRulesAsync();
        Task<IEnumerable<KeywordSensitivityRule>> SearchAndFilterRulesAsync(string? query, string? categoryFilter, string? severityFilter);
        Task AddRuleAsync(KeywordSensitivityRule rule);
        Task UpdateRuleAsync(KeywordSensitivityRule rule);
        Task DeleteRuleAsync(int ruleId);
        Task<TextAnalysisResult> AnalyzeIncidentTextAsync(string description);
        Task<string> GenerateRuleIdAsync();

        // Location Weight Rules
        Task<IEnumerable<LocationWeightRule>> GetAllLocationRulesAsync();
        Task<IEnumerable<LocationWeightRule>> SearchAndFilterLocationRulesAsync(string? query, string? categoryFilter, string? supervisionFilter);
        Task AddLocationRuleAsync(LocationWeightRule rule);
        Task UpdateLocationRuleAsync(LocationWeightRule rule);
        Task DeleteLocationRuleAsync(int ruleId);
        Task<string> GenerateLocationCodeAsync();
        Task<TextAnalysisResult> CalculateCompositeRiskAsync(string description, string locationCode);

        // Repeat Offender Rules Engine
        Task<IEnumerable<RepeatOffenderRule>> GetAllRepeatOffenderRulesAsync();
        Task<IEnumerable<RepeatOffenderRule>> SearchAndFilterRepeatOffenderRulesAsync(string? query, string? categoryFilter);
        Task AddRepeatOffenderRuleAsync(RepeatOffenderRule rule);
        Task UpdateRepeatOffenderRuleAsync(RepeatOffenderRule rule);
        Task DeleteRepeatOffenderRuleAsync(int ruleId);
        Task<string> GeneratePolicyIdAsync();
        Task<TextAnalysisResult> EvaluateCompleteIncidentRiskAsync(string studentNameOrId, string category, string description, string locationCode);
    }
}