using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class KeywordSensitivityRule : EntityBase
    {
        public string RuleId { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public string Category { get; set; } = "Violence & Altercation"; // Violence & Altercation, Weapons & Threats, Substance & Contraband, Bullying & Harassment, Vandalism & Property, Academic & Protocol
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        public int WeightScore { get; set; } = 25; // 1 to 100
        public string MatchType { get; set; } = "Contains Word Root"; // Exact Match, Contains Word Root, Regex Pattern
        public bool AutoEscalate { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}