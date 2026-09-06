using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class IncidentClassificationService : IIncidentClassificationService
    {
        private readonly AppDbContext _context;

        public IncidentClassificationService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // --- 1. Keyword Sensitivity Rules ---
        public async Task<IEnumerable<KeywordSensitivityRule>> GetAllRulesAsync()
        {
            return await _context.KeywordSensitivityRules.OrderByDescending(r => r.WeightScore).ToListAsync();
        }

        public async Task<IEnumerable<KeywordSensitivityRule>> SearchAndFilterRulesAsync(string? query, string? categoryFilter, string? severityFilter)
        {
            var q = _context.KeywordSensitivityRules.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(r => r.Keyword.ToLower().Contains(term) || r.Category.ToLower().Contains(term) || r.RuleId.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "All" && categoryFilter != "All Categories")
            {
                q = q.Where(r => r.Category.ToLower().Contains(categoryFilter.Trim().ToLower()));
            }
            if (!string.IsNullOrWhiteSpace(severityFilter) && severityFilter != "All" && severityFilter != "All Severities")
            {
                q = q.Where(r => r.Severity.ToLower() == severityFilter.Trim().ToLower());
            }
            return await q.OrderByDescending(r => r.WeightScore).ToListAsync();
        }

        public async Task AddRuleAsync(KeywordSensitivityRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.RuleId)) rule.RuleId = await GenerateRuleIdAsync();
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            await _context.KeywordSensitivityRules.AddAsync(rule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRuleAsync(KeywordSensitivityRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var existing = await _context.KeywordSensitivityRules.FindAsync(rule.Id);
            if (existing != null)
            {
                existing.Keyword = rule.Keyword;
                existing.Category = rule.Category;
                existing.Severity = rule.Severity;
                existing.WeightScore = rule.WeightScore;
                existing.MatchType = rule.MatchType;
                existing.AutoEscalate = rule.AutoEscalate;
                existing.IsActive = rule.IsActive;
                existing.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRuleAsync(int ruleId)
        {
            var rule = await _context.KeywordSensitivityRules.FindAsync(ruleId);
            if (rule != null) { _context.KeywordSensitivityRules.Remove(rule); await _context.SaveChangesAsync(); }
        }

        public async Task<TextAnalysisResult> AnalyzeIncidentTextAsync(string description)
        {
            return await CalculateCompositeRiskAsync(description, string.Empty);
        }

        public async Task<string> GenerateRuleIdAsync()
        {
            int count = await _context.KeywordSensitivityRules.CountAsync() + 1;
            return $"KWD-{count:D3}";
        }

        // --- 2. Location Weight Rules ---
        public async Task<IEnumerable<LocationWeightRule>> GetAllLocationRulesAsync()
        {
            return await _context.LocationWeightRules.OrderByDescending(l => l.RiskMultiplier).ToListAsync();
        }

        public async Task<IEnumerable<LocationWeightRule>> SearchAndFilterLocationRulesAsync(string? query, string? categoryFilter, string? supervisionFilter)
        {
            var q = _context.LocationWeightRules.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(l => l.LocationName.ToLower().Contains(term) || l.Building.ToLower().Contains(term) || l.LocationCode.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "All" && categoryFilter != "All Categories")
            {
                q = q.Where(l => l.Category.ToLower().Contains(categoryFilter.Trim().ToLower()));
            }
            if (!string.IsNullOrWhiteSpace(supervisionFilter) && supervisionFilter != "All" && supervisionFilter != "All Supervision")
            {
                string sup = supervisionFilter.Trim().ToLower();
                if (sup.Contains("blind")) q = q.Where(l => l.SupervisionLevel.ToLower().Contains("blind"));
                else if (sup.Contains("periodic")) q = q.Where(l => l.SupervisionLevel.ToLower().Contains("periodic"));
                else q = q.Where(l => l.SupervisionLevel.ToLower().Contains("high"));
            }
            return await q.OrderByDescending(l => l.RiskMultiplier).ToListAsync();
        }

        public async Task AddLocationRuleAsync(LocationWeightRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.LocationCode)) rule.LocationCode = await GenerateLocationCodeAsync();
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            await _context.LocationWeightRules.AddAsync(rule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLocationRuleAsync(LocationWeightRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var existing = await _context.LocationWeightRules.FindAsync(rule.Id);
            if (existing != null)
            {
                existing.LocationName = rule.LocationName;
                existing.Building = rule.Building;
                existing.Category = rule.Category;
                existing.SupervisionLevel = rule.SupervisionLevel;
                existing.HasCCTV = rule.HasCCTV;
                existing.RiskMultiplier = rule.RiskMultiplier;
                existing.IsRestricted = rule.IsRestricted;
                existing.IsActive = rule.IsActive;
                existing.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteLocationRuleAsync(int ruleId)
        {
            var rule = await _context.LocationWeightRules.FindAsync(ruleId);
            if (rule != null) { _context.LocationWeightRules.Remove(rule); await _context.SaveChangesAsync(); }
        }

        public async Task<string> GenerateLocationCodeAsync()
        {
            int count = await _context.LocationWeightRules.CountAsync() + 1;
            return $"LOC-{count:D3}";
        }

        public async Task<TextAnalysisResult> CalculateCompositeRiskAsync(string description, string locationCode)
        {
            return await EvaluateCompleteIncidentRiskAsync(string.Empty, string.Empty, description, locationCode);
        }

        // --- 3. Repeat Offender Rules & Complete Multi-Layer Evaluation Engine ---
        public async Task<IEnumerable<RepeatOffenderRule>> GetAllRepeatOffenderRulesAsync()
        {
            return await _context.RepeatOffenderRules.OrderBy(r => r.PolicyId).ToListAsync();
        }

        public async Task<IEnumerable<RepeatOffenderRule>> SearchAndFilterRepeatOffenderRulesAsync(string? query, string? categoryFilter)
        {
            var q = _context.RepeatOffenderRules.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(r => r.PolicyName.ToLower().Contains(term) || r.Category.ToLower().Contains(term) || r.PolicyId.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "All" && categoryFilter != "All Categories")
            {
                q = q.Where(r => r.Category.ToLower().Contains(categoryFilter.Trim().ToLower()));
            }
            return await q.OrderBy(r => r.PolicyId).ToListAsync();
        }

        public async Task AddRepeatOffenderRuleAsync(RepeatOffenderRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.PolicyId)) rule.PolicyId = await GeneratePolicyIdAsync();
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            await _context.RepeatOffenderRules.AddAsync(rule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRepeatOffenderRuleAsync(RepeatOffenderRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var existing = await _context.RepeatOffenderRules.FindAsync(rule.Id);
            if (existing != null)
            {
                existing.PolicyName = rule.PolicyName;
                existing.Category = rule.Category;
                existing.LookbackDays = rule.LookbackDays;
                existing.ThresholdCount = rule.ThresholdCount;
                existing.PenaltyType = rule.PenaltyType;
                existing.BonusPoints = rule.BonusPoints;
                existing.AutoSummons = rule.AutoSummons;
                existing.FlagProfile = rule.FlagProfile;
                existing.RequireDeanSignoff = rule.RequireDeanSignoff;
                existing.IsActive = rule.IsActive;
                existing.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRepeatOffenderRuleAsync(int ruleId)
        {
            var rule = await _context.RepeatOffenderRules.FindAsync(ruleId);
            if (rule != null) { _context.RepeatOffenderRules.Remove(rule); await _context.SaveChangesAsync(); }
        }

        public async Task<string> GeneratePolicyIdAsync()
        {
            int count = await _context.RepeatOffenderRules.CountAsync() + 1;
            return $"REC-{count:D3}";
        }

        public async Task<TextAnalysisResult> EvaluateCompleteIncidentRiskAsync(string studentNameOrId, string category, string description, string locationCode)
        {
            var result = new TextAnalysisResult();
            if (string.IsNullOrWhiteSpace(description)) { result.SummaryMessage = "No narrative provided."; return result; }

            // Layer 1: Keyword Analysis
            var activeKeywords = await _context.KeywordSensitivityRules.Where(r => r.IsActive).ToListAsync();
            string text = description.ToLower();
            int baseScore = 0;
            bool autoEscalate = false;
            var matchedRules = new List<KeywordSensitivityRule>();

            foreach (var rule in activeKeywords)
            {
                bool isMatch = false;
                string kw = rule.Keyword.Trim().ToLower();
                if (rule.MatchType == "Exact Match") isMatch = Regex.IsMatch(text, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase);
                else if (rule.MatchType == "Regex Pattern") { try { isMatch = Regex.IsMatch(text, kw, RegexOptions.IgnoreCase); } catch { isMatch = text.Contains(kw); } }
                else isMatch = text.Contains(kw);

                if (isMatch)
                {
                    matchedRules.Add(rule);
                    result.FlaggedKeywords.Add(rule.Keyword);
                    baseScore += rule.WeightScore;
                    if (rule.AutoEscalate) autoEscalate = true;
                }
            }

            // Layer 2: Location Multiplier
            double multiplier = 1.0;
            if (!string.IsNullOrWhiteSpace(locationCode))
            {
                var locRule = await _context.LocationWeightRules.FirstOrDefaultAsync(l => l.LocationCode.ToLower() == locationCode.Trim().ToLower() && l.IsActive);
                if (locRule != null)
                {
                    multiplier = locRule.RiskMultiplier;
                    if (locRule.IsRestricted) autoEscalate = true;
                }
            }

            int subtotalScore = (int)Math.Round(baseScore * multiplier);

            // Layer 3: Recidivism / Repeat Offender Check
            int bonusPoints = 0;
            if (!string.IsNullOrWhiteSpace(studentNameOrId))
            {
                var activeRecidivismRules = await _context.RepeatOffenderRules.Where(r => r.IsActive).ToListAsync();
                foreach (var recRule in activeRecidivismRules)
                {
                    DateTime cutoff = DateTime.Now.AddDays(-recRule.LookbackDays);
                    int pastIncidentsCount = await _context.Incidents.CountAsync(i =>
                        i.PrimaryInvolvedStudent.ToLower().Contains(studentNameOrId.Trim().ToLower()) &&
                        i.IncidentDate >= cutoff);

                    if (pastIncidentsCount >= recRule.ThresholdCount)
                    {
                        if (recRule.PenaltyType == "Force Critical Tier Immediately") autoEscalate = true;
                        else if (recRule.PenaltyType == "Bump Severity Tier by +1") bonusPoints += 30;
                        else bonusPoints += recRule.BonusPoints;
                    }
                }
            }

            int finalScore = Math.Min(100, subtotalScore + bonusPoints);
            result.TotalScore = finalScore;
            result.RequiresImmediateEscalation = autoEscalate;

            if (matchedRules.Any()) result.SuggestedCategory = matchedRules.OrderByDescending(r => r.WeightScore).First().Category;
            else if (!string.IsNullOrWhiteSpace(category)) result.SuggestedCategory = category;
            else result.SuggestedCategory = "General Disciplinary";

            if (autoEscalate || result.TotalScore >= 85) result.SuggestedSeverity = "Critical";
            else if (result.TotalScore >= 60) result.SuggestedSeverity = "High";
            else if (result.TotalScore >= 30) result.SuggestedSeverity = "Medium";
            else result.SuggestedSeverity = "Low";

            result.SummaryMessage = $"Evaluated complete risk engine (Keywords: {baseScore}, Loc Multiplier: {multiplier:F1}x, Recidivism Bonus: +{bonusPoints}). Final Risk Score: {result.TotalScore}/100.";
            return result;
        }
    }
}