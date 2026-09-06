using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class RuleEngineService : IRuleEngineService
    {
        private readonly AppDbContext _context;

        private static readonly string[] StandardCategories = new[]
        {
            "Bullying",
            "Cyberbullying",
            "Physical Altercation",
            "Verbal Abuse",
            "Harassment",
            "Theft",
            "Vandalism",
            "Truancy",
            "Fighting",
            "Threats & Intimidation",
            "Substance Misconduct",
            "Property Damage",
            "Classroom Disruption",
            "General Misconduct"
        };

        private static readonly Dictionary<string, (string Category, IncidentSeverity BaseSeverity, RiskLevel BaseRisk)> KeywordRules =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "weapon", ("Threats & Intimidation", IncidentSeverity.Critical, RiskLevel.Critical) },
            { "gun", ("Threats & Intimidation", IncidentSeverity.Critical, RiskLevel.Critical) },
            { "knife", ("Threats & Intimidation", IncidentSeverity.Critical, RiskLevel.Critical) },
            { "kill", ("Threats & Intimidation", IncidentSeverity.Critical, RiskLevel.Critical) },
            { "assault", ("Physical Altercation", IncidentSeverity.Critical, RiskLevel.Critical) },
            { "blood", ("Physical Altercation", IncidentSeverity.High, RiskLevel.High) },
            { "punch", ("Fighting", IncidentSeverity.High, RiskLevel.High) },
            { "punched", ("Fighting", IncidentSeverity.High, RiskLevel.High) },
            { "fight", ("Fighting", IncidentSeverity.High, RiskLevel.High) },
            { "scuffle", ("Physical Altercation", IncidentSeverity.High, RiskLevel.High) },
            { "hit", ("Physical Altercation", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "slap", ("Physical Altercation", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "threat", ("Threats & Intimidation", IncidentSeverity.High, RiskLevel.High) },
            { "threatened", ("Threats & Intimidation", IncidentSeverity.High, RiskLevel.High) },
            { "bully", ("Bullying", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "bullying", ("Bullying", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "cyberbullying", ("Cyberbullying", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "online chat", ("Cyberbullying", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "harass", ("Harassment", IncidentSeverity.High, RiskLevel.High) },
            { "harassment", ("Harassment", IncidentSeverity.High, RiskLevel.High) },
            { "insult", ("Verbal Abuse", IncidentSeverity.Low, RiskLevel.Low) },
            { "verbal", ("Verbal Abuse", IncidentSeverity.Low, RiskLevel.Low) },
            { "yelling", ("Verbal Abuse", IncidentSeverity.Low, RiskLevel.Low) },
            { "stole", ("Theft", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "steal", ("Theft", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "theft", ("Theft", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "vandalism", ("Vandalism", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "graffiti", ("Vandalism", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "damage", ("Property Damage", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "break", ("Property Damage", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "truancy", ("Truancy", IncidentSeverity.Low, RiskLevel.Low) },
            { "cutting class", ("Truancy", IncidentSeverity.Low, RiskLevel.Low) },
            { "detention", ("Truancy", IncidentSeverity.Low, RiskLevel.Low) },
            { "skip", ("Truancy", IncidentSeverity.Low, RiskLevel.Low) },
            { "disruption", ("Classroom Disruption", IncidentSeverity.Low, RiskLevel.Low) },
            { "disrupt", ("Classroom Disruption", IncidentSeverity.Low, RiskLevel.Low) },
            { "paper airplane", ("Classroom Disruption", IncidentSeverity.Low, RiskLevel.Low) },
            { "drug", ("Substance Misconduct", IncidentSeverity.Critical, RiskLevel.Critical) },
            { "alcohol", ("Substance Misconduct", IncidentSeverity.High, RiskLevel.High) },
            { "smoke", ("Substance Misconduct", IncidentSeverity.Medium, RiskLevel.Medium) },
            { "vape", ("Substance Misconduct", IncidentSeverity.Medium, RiskLevel.Medium) }
        };

        public RuleEngineService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<string> GetStandardCategories() => StandardCategories;

        public async Task<string> GenerateIncidentNumberAsync()
        {
            int year = DateTime.Now.Year;
            int count = await _context.Incidents.CountAsync() + 1;
            return $"INC-{year}-{count:D5}";
        }

        public async Task<RuleAnalysisResult> AnalyzeAsync(string narrative, string location, string? studentName = null, string? userCategory = null, int? studentId = null)
        {
            var result = new RuleAnalysisResult();
            var text = (narrative ?? string.Empty).ToLower();
            var loc = (location ?? string.Empty).ToLower();

            string detectedCategory = userCategory ?? "General Misconduct";
            IncidentSeverity highestSeverity = IncidentSeverity.Low;
            RiskLevel highestRisk = RiskLevel.Low;

            // 1. Scan keywords in narrative
            foreach (var kvp in KeywordRules)
            {
                if (text.Contains(kvp.Key))
                {
                    result.TriggeredRules.Add($"Matched behavioral keyword: \"{kvp.Key}\"");
                    if (string.IsNullOrEmpty(userCategory) || detectedCategory == "General Misconduct")
                    {
                        detectedCategory = kvp.Value.Category;
                    }

                    if (kvp.Value.BaseSeverity > highestSeverity)
                    {
                        highestSeverity = kvp.Value.BaseSeverity;
                    }

                    if (kvp.Value.BaseRisk > highestRisk)
                    {
                        highestRisk = kvp.Value.BaseRisk;
                    }
                }
            }

            // 2. High-risk location weighting (Restrooms, Locker rooms, Off-campus corners)
            if (loc.Contains("restroom") || loc.Contains("bathroom") || loc.Contains("locker"))
            {
                result.TriggeredRules.Add("Sensitive/unmonitored location detected (Elevated supervision factor)");
                if (highestRisk < RiskLevel.Medium) highestRisk = RiskLevel.Medium;
            }

            // 3. Repeat incident check for student in past 30 days
            var cutoff = DateTime.Now.AddDays(-30);
            var query = _context.Incidents.AsQueryable().Where(i => i.IncidentDate >= cutoff);

            bool checkStudent = false;
            string sName = (studentName ?? string.Empty).Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(sName) && sName.Length >= 2)
            {
                checkStudent = true;
                query = query.Where(i =>
                    (i.PrimaryInvolvedStudent != null && i.PrimaryInvolvedStudent.ToLower().Contains(sName)) ||
                    (i.Student != null && (i.Student.FirstName.ToLower() + " " + i.Student.LastName.ToLower()).Contains(sName)));
            }
            else if (studentId.HasValue && studentId.Value > 0)
            {
                checkStudent = true;
                query = query.Where(i => i.StudentId == studentId.Value);
            }

            if (checkStudent)
            {
                var priorIncidents = await query.ToListAsync();
                result.RepeatCount = priorIncidents.Count;
                if (result.RepeatCount > 0)
                {
                    result.IsRepeatOffender = true;
                    result.RepeatAlertMessage = $"Repeat Pattern: Student has {result.RepeatCount} other incident(s) recorded in the past 30 days.";
                    result.TriggeredRules.Add($"Recurrent behavior: {result.RepeatCount} incident(s) in last 30 days.");

                    // Elevate risk if repeated
                    if (result.RepeatCount >= 2 && highestRisk < RiskLevel.High)
                    {
                        highestRisk = RiskLevel.High;
                        result.TriggeredRules.Add("Risk escalated to HIGH due to recurrent violations (3+ in 30 days).");
                    }
                    else if (result.RepeatCount >= 1 && highestRisk < RiskLevel.Medium)
                    {
                        highestRisk = RiskLevel.Medium;
                        result.TriggeredRules.Add("Risk escalated to MEDIUM due to prior violation in last 30 days.");
                    }
                }
            }

            result.SuggestedCategory = detectedCategory;
            result.SuggestedSeverity = highestSeverity;
            result.SuggestedRisk = highestRisk;
            result.RecommendedAction = GenerateRecommendedAction(highestRisk, detectedCategory, result.IsRepeatOffender);

            return result;
        }

        private static string GenerateRecommendedAction(RiskLevel risk, string category, bool isRepeat)
        {
            return risk switch
            {
                RiskLevel.Critical => "IMMEDIATE ACTION: Escalate to School Principal & Security Officer immediately. Summon parents/guardians and initiate emergency disciplinary review.",
                RiskLevel.High => isRepeat
                    ? "Mandatory formal investigation. Schedule parent conference and issue formal guidance counselor referral with behavioral contract."
                    : "Initiate formal incident investigation, parent notification, and guidance counselor evaluation.",
                RiskLevel.Medium => isRepeat
                    ? "Parent notification required. Refer student to guidance counseling and assign restorative school service."
                    : "Issue written reprimand, teacher conference, and parent notification.",
                _ => "Verbal reprimand, teacher-student counseling, and 14-day behavior monitoring."
            };
        }
    }
}