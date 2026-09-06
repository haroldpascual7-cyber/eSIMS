using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class RuleAnalysisResult
    {
        public string SuggestedCategory { get; set; } = "General Misconduct";
        public IncidentSeverity SuggestedSeverity { get; set; } = IncidentSeverity.Low;
        public RiskLevel SuggestedRisk { get; set; } = RiskLevel.Low;
        public string RecommendedAction { get; set; } = string.Empty;
        public bool IsRepeatOffender { get; set; }
        public int RepeatCount { get; set; }
        public string RepeatAlertMessage { get; set; } = string.Empty;
        public List<string> TriggeredRules { get; set; } = new List<string>();
        public double ConfidenceScore { get; set; } = 1.0;
    }

    public interface IRuleEngineService
    {
        Task<RuleAnalysisResult> AnalyzeAsync(string narrative, string location, string? studentName = null, string? userCategory = null, int? studentId = null);
        Task<string> GenerateIncidentNumberAsync();
        IEnumerable<string> GetStandardCategories();
    }
}