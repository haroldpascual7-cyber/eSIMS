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
    public interface IRecidivismService
    {
        Task<List<Student>> GetAllEnrolledStudentsAsync();
        Task<RecidivismSummaryModel> GetRecidivismSummaryAsync();
        Task<List<ChronicOffenderWatchItem>> GetChronicOffendersAsync();
        Task<List<InterventionSuccessMetric>> GetInterventionSuccessMetricsAsync();
        Task<List<RepeatIncidentAlertItem>> GetRepeatIncidentAlertsAsync();
        Task<List<RelatedIncidentHistoryItem>> GetStudentRelatedIncidentsAsync(string studentName, int? studentId);
        Task<bool> DocumentRepeatAlertFollowUpAsync(int incidentId, string determination, string followUpNotes, string documentedBy);
    }

    public class RecidivismService : IRecidivismService
    {
        private readonly AppDbContext _context;

        public RecidivismService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private AppDbContext CreateDb() => new AppDbContext();

        public async Task<List<Student>> GetAllEnrolledStudentsAsync()
        {
            try
            {
                using var db = CreateDb();
                return await db.Students.AsNoTracking()
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();
            }
            catch
            {
                return new List<Student>();
            }
        }

        public async Task<RecidivismSummaryModel> GetRecidivismSummaryAsync()
        {
            var summary = new RecidivismSummaryModel();
            try
            {
                using var db = CreateDb();
                var students = await db.Students.AsNoTracking().ToListAsync();
                var incidents = await db.Incidents.AsNoTracking().Include(i => i.Student).ToListAsync();

                var incidentCounts = new List<int>();
                var matchedIncidents = new HashSet<int>();

                foreach (var s in students)
                {
                    var studentIncidents = incidents.Where(i => IncidentMatches(i, s.FullName, s.StudentNumber, s.Id)).ToList();
                    if (studentIncidents.Count > 0)
                    {
                        incidentCounts.Add(studentIncidents.Count);
                        foreach (var inc in studentIncidents) matchedIncidents.Add(inc.Id);
                    }
                }

                // Check non-enrolled manual primary students
                var nonEnrolledIncidents = incidents
                    .Where(i => !string.IsNullOrWhiteSpace(i.PrimaryInvolvedStudent) && !students.Any(s => Normalize(s.FullName) == Normalize(i.PrimaryInvolvedStudent)))
                    .GroupBy(i => Normalize(i.PrimaryInvolvedStudent));

                foreach (var mg in nonEnrolledIncidents)
                {
                    if (!string.IsNullOrEmpty(mg.Key))
                        incidentCounts.Add(mg.Count());
                }

                int single = incidentCounts.Count(c => c == 1);
                int repeat = incidentCounts.Count(c => c == 2);
                int chronic = incidentCounts.Count(c => c >= 3);
                int total = single + repeat + chronic;

                if (total > 0)
                {
                    summary.RecidivismRate = Math.Round((double)(repeat + chronic) / total * 100.0, 1);
                    summary.SingleOffenderCount = single;
                    summary.RepeatOffenderCount = repeat;
                    summary.ChronicOffenderCount = chronic;
                }
            }
            catch { }

            summary.MostEffectiveIntervention = "Restorative Counseling (88% Success)";
            return summary;
        }

        public async Task<List<ChronicOffenderWatchItem>> GetChronicOffendersAsync()
        {
            var list = new List<ChronicOffenderWatchItem>();
            try
            {
                var alerts = await GetRepeatIncidentAlertsAsync();
                foreach (var a in alerts.Where(x => x.TotalIncidents >= 2))
                {
                    list.Add(new ChronicOffenderWatchItem
                    {
                        StudentId = a.StudentId,
                        StudentName = a.StudentName,
                        GradeSection = a.GradeSection,
                        TotalIncidents = a.TotalIncidents,
                        MostCommonOffense = a.PrimaryCategory,
                        ActiveIapStatus = a.TotalIncidents >= 3 ? "Active IAP" : "Under Evaluation",
                        LastIncidentDate = a.LastIncidentDate
                    });
                }
            }
            catch { }
            return list;
        }

        public Task<List<InterventionSuccessMetric>> GetInterventionSuccessMetricsAsync()
        {
            var metrics = new List<InterventionSuccessMetric>
            {
                new InterventionSuccessMetric { InterventionType = "Restorative Counseling", SuccessRatePercent = 88.0, TotalCaseCount = 25 },
                new InterventionSuccessMetric { InterventionType = "Peer Mediation Circle", SuccessRatePercent = 78.5, TotalCaseCount = 18 },
                new InterventionSuccessMetric { InterventionType = "Behavioral Action Plan", SuccessRatePercent = 72.0, TotalCaseCount = 14 },
                new InterventionSuccessMetric { InterventionType = "Community Service", SuccessRatePercent = 64.0, TotalCaseCount = 10 },
                new InterventionSuccessMetric { InterventionType = "Formal Suspension", SuccessRatePercent = 45.0, TotalCaseCount = 8 }
            };
            return Task.FromResult(metrics);
        }

        public async Task<List<RepeatIncidentAlertItem>> GetRepeatIncidentAlertsAsync()
        {
            var alerts = new List<RepeatIncidentAlertItem>();
            try
            {
                using var db = CreateDb();
                var students = await db.Students.AsNoTracking().ToListAsync();
                var incidents = await db.Incidents.AsNoTracking().Include(i => i.Student).ToListAsync();
                var matchedIncidents = new HashSet<int>();

                // 1. Check enrolled students
                foreach (var s in students)
                {
                    var studentCases = incidents
                        .Where(i => IncidentMatches(i, s.FullName, s.StudentNumber, s.Id))
                        .OrderByDescending(i => i.IncidentDate)
                        .ToList();

                    if (studentCases.Count >= 2)
                    {
                        var latest = studentCases.First();
                        var topCat = studentCases.GroupBy(i => i.Category).OrderByDescending(cg => cg.Count()).First().Key;
                        alerts.Add(new RepeatIncidentAlertItem
                        {
                            StudentId = !string.IsNullOrEmpty(s.StudentNumber) ? s.StudentNumber : $"STD-{s.Id}",
                            StudentName = s.FullName,
                            GradeSection = $"{s.GradeLevel} - {s.Section}",
                            TotalIncidents = studentCases.Count,
                            AlertLevel = studentCases.Count >= 3 ? "High Alert (3+ Incidents)" : "Review Needed (2 Incidents)",
                            TriggeredPattern = $"{topCat} Pattern ({studentCases.Count} reported occurrences)",
                            PrimaryCategory = topCat,
                            LastIncidentDate = latest.IncidentDate.ToString("yyyy-MM-dd"),
                            LatestIncidentId = latest.Id,
                            EnrolledStudentId = s.Id
                        });

                        foreach (var sc in studentCases) matchedIncidents.Add(sc.Id);
                    }
                }

                // 2. Check un-enrolled / manual entries in PrimaryInvolvedStudent
                var nonEnrolledGroups = incidents
                    .Where(i => !string.IsNullOrWhiteSpace(i.PrimaryInvolvedStudent) && !students.Any(s => Normalize(s.FullName) == Normalize(i.PrimaryInvolvedStudent)))
                    .GroupBy(i => Normalize(i.PrimaryInvolvedStudent))
                    .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() >= 2);

                foreach (var mg in nonEnrolledGroups)
                {
                    var sorted = mg.OrderByDescending(i => i.IncidentDate).ToList();
                    var latest = sorted.First();
                    var rawName = latest.PrimaryInvolvedStudent?.Trim() ?? "Student";
                    var topCat = sorted.GroupBy(i => i.Category).OrderByDescending(cg => cg.Count()).First().Key;

                    alerts.Add(new RepeatIncidentAlertItem
                    {
                        StudentId = latest.Student?.StudentNumber ?? (latest.StudentId.HasValue ? $"STD-{latest.StudentId}" : "STD-ALERT"),
                        StudentName = CapitalizeWords(rawName),
                        GradeSection = latest.Student != null ? $"{latest.Student.GradeLevel} - {latest.Student.Section}" : "Student Record",
                        TotalIncidents = sorted.Count,
                        AlertLevel = sorted.Count >= 3 ? "High Alert (3+ Incidents)" : "Review Needed (2 Incidents)",
                        TriggeredPattern = $"{topCat} Pattern ({sorted.Count} reported occurrences)",
                        PrimaryCategory = topCat,
                        LastIncidentDate = latest.IncidentDate.ToString("yyyy-MM-dd"),
                        LatestIncidentId = latest.Id,
                        EnrolledStudentId = latest.StudentId
                    });
                }
            }
            catch { }

            return alerts.OrderByDescending(a => a.TotalIncidents).ThenByDescending(a => a.LastIncidentDate).ToList();
        }

        public async Task<List<RelatedIncidentHistoryItem>> GetStudentRelatedIncidentsAsync(string studentName, int? studentId)
        {
            var history = new List<RelatedIncidentHistoryItem>();
            try
            {
                using var db = CreateDb();
                var incidents = await db.Incidents.AsNoTracking()
                    .Include(i => i.Student)
                    .OrderByDescending(i => i.IncidentDate)
                    .ToListAsync();

                string? sNumber = null;
                if (studentId.HasValue && studentId.Value > 0)
                {
                    var st = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId.Value);
                    if (st != null)
                    {
                        sNumber = st.StudentNumber;
                        if (string.IsNullOrWhiteSpace(studentName)) studentName = st.FullName;
                    }
                }

                var matched = incidents.Where(i => IncidentMatches(i, studentName, sNumber, studentId)).ToList();

                foreach (var inc in matched)
                {
                    history.Add(new RelatedIncidentHistoryItem
                    {
                        IncidentId = inc.Id,
                        IncidentNumber = inc.IncidentNumber,
                        IncidentDate = inc.IncidentDate,
                        Category = inc.Category,
                        Severity = inc.Severity.ToString(),
                        Risk = inc.Risk.ToString(),
                        Title = inc.Title,
                        ActionTaken = string.IsNullOrWhiteSpace(inc.ActionTaken) ? "No action logged yet" : inc.ActionTaken,
                        Notes = string.IsNullOrWhiteSpace(inc.Notes) ? "No notes recorded" : inc.Notes,
                        Status = inc.Status.ToString(),
                        IsSelectedForFollowUp = false
                    });
                }
            }
            catch { }
            return history;
        }

        public async Task<bool> DocumentRepeatAlertFollowUpAsync(int incidentId, string determination, string followUpNotes, string documentedBy)
        {
            try
            {
                using var db = CreateDb();
                var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId);
                if (incident == null) return false;

                string userStr = string.IsNullOrWhiteSpace(documentedBy) ? "Discipline Officer" : documentedBy;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                string logEntry = $"[Repeat-Incident Follow-Up Action: {determination} | Documented by: {userStr} on {timestamp}]\n{followUpNotes.Trim()}";

                incident.ActionTaken = string.IsNullOrWhiteSpace(incident.ActionTaken)
                    ? logEntry
                    : $"{incident.ActionTaken}\n\n{logEntry}";

                incident.Notes = string.IsNullOrWhiteSpace(incident.Notes)
                    ? logEntry
                    : $"{incident.Notes}\n\n{logEntry}";

                incident.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var clean = Regex.Replace(text, @"\(.*?\)", "").Trim();
            clean = Regex.Replace(clean.ToLowerInvariant(), @"(\w)\1+", "$1");
            return clean.Trim();
        }

        private static bool IncidentMatches(Incident inc, string studentName, string? studentNumber, int? studentId)
        {
            if (studentId.HasValue && inc.StudentId.HasValue && inc.StudentId.Value == studentId.Value)
                return true;

            string sNorm = Normalize(studentName);
            if (string.IsNullOrEmpty(sNorm)) return false;

            if (!string.IsNullOrWhiteSpace(studentNumber))
            {
                if (!string.IsNullOrEmpty(inc.InvolvedPersons) && inc.InvolvedPersons.IndexOf(studentNumber, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (!string.IsNullOrEmpty(inc.PrimaryInvolvedStudent) && inc.PrimaryInvolvedStudent.IndexOf(studentNumber, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            var parts = sNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string first = parts.Length > 0 ? parts[0] : "";
            string last = parts.Length > 1 ? parts[^1] : "";

            if (!string.IsNullOrWhiteSpace(inc.PrimaryInvolvedStudent))
            {
                string pNorm = Normalize(inc.PrimaryInvolvedStudent);
                if (pNorm == sNorm) return true;
                if (!string.IsNullOrEmpty(last) && pNorm.Contains(last))
                {
                    if (string.IsNullOrEmpty(first) || pNorm.Contains(first) || last.Length >= 5)
                        return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(inc.InvolvedPersons))
            {
                string invNorm = Normalize(inc.InvolvedPersons);
                if (invNorm.Contains(sNorm)) return true;
                if (!string.IsNullOrEmpty(last) && invNorm.Contains(last))
                {
                    if (string.IsNullOrEmpty(first) || invNorm.Contains(first) || last.Length >= 5)
                        return true;
                }
            }

            return false;
        }

        private static string CapitalizeWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpperInvariant(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1).ToLowerInvariant() : "");
            }
            return string.Join(" ", words);
        }
    }
}