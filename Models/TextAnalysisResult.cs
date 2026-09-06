using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class TextAnalysisResult
    {
        public List<string> FlaggedKeywords { get; set; } = new();
        public int TotalScore { get; set; } = 0;
        public string SuggestedSeverity { get; set; } = "Low"; // Low, Medium, High, Critical
        public string SuggestedCategory { get; set; } = "General Disciplinary";
        public bool RequiresImmediateEscalation { get; set; } = false;
        public string SummaryMessage { get; set; } = string.Empty;
    }
}