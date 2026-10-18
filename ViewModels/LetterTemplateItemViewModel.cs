using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Linq;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class LetterTemplateItemViewModel : ObservableObject
    {
        public LetterTemplate Template { get; }

        public LetterTemplateItemViewModel(LetterTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
        }

        public string Id => Template.Id;
        public string TemplateName => Template.TemplateName;
        public string TemplateBody => Template.TemplateBody;

        public string ReferenceCode
        {
            get
            {
                if (TemplateName.Contains("Suspension", StringComparison.OrdinalIgnoreCase))
                    return "REF: SA-DISC-2026-SUSP";
                if (TemplateName.Contains("Conference", StringComparison.OrdinalIgnoreCase) || TemplateName.Contains("Summons", StringComparison.OrdinalIgnoreCase))
                    return "REF: SA-SUMM-2026-CONF";
                if (TemplateName.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                    return "REF: SA-ADVI-2026-WARN";
                return $"REF: SA-NOT-{Id.ToUpper()}";
            }
        }

        public string DocumentBadge
        {
            get
            {
                if (TemplateName.Contains("Suspension", StringComparison.OrdinalIgnoreCase))
                    return "DISCIPLINARY SUSPENSION ORDER";
                if (TemplateName.Contains("Conference", StringComparison.OrdinalIgnoreCase) || TemplateName.Contains("Summons", StringComparison.OrdinalIgnoreCase))
                    return "MANDATORY PARENT CONFERENCE";
                if (TemplateName.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                    return "FORMAL CONDUCT ADVISORY";
                return "OFFICIAL SCHOOL DISCLOSURE";
            }
        }

        public string Icon
        {
            get
            {
                if (TemplateName.Contains("Suspension", StringComparison.OrdinalIgnoreCase))
                    return "🛡️";
                if (TemplateName.Contains("Conference", StringComparison.OrdinalIgnoreCase) || TemplateName.Contains("Summons", StringComparison.OrdinalIgnoreCase))
                    return "⚖️";
                if (TemplateName.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                    return "📜";
                return "📄";
            }
        }

        public string CategorySummary
        {
            get
            {
                if (TemplateName.Contains("Suspension", StringComparison.OrdinalIgnoreCase))
                    return "Formal notice of class suspension, required conditions, and re-entry counseling.";
                if (TemplateName.Contains("Conference", StringComparison.OrdinalIgnoreCase) || TemplateName.Contains("Summons", StringComparison.OrdinalIgnoreCase))
                    return "Mandatory summon requiring guardian attendance for case review and corrective action.";
                if (TemplateName.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                    return "Official conduct advisory addressing behavioral violations and monitoring terms.";
                return "Official correspondence template issued by the Office of Student Affairs.";
            }
        }

        public string SampleExcerpt
        {
            get
            {
                string body = Template.TemplateBody;
                if (string.IsNullOrWhiteSpace(body)) return string.Empty;

                string sample = body
                    .Replace("{{StudentName}}", "Juan Dela Cruz")
                    .Replace("{{StudentId}}", "STD-1042")
                    .Replace("{{GradeSection}}", "Grade 10 - Rizal")
                    .Replace("{{ActionDate}}", DateTime.Today.AddDays(2).ToString("MMM dd, yyyy"))
                    .Replace("{{IncidentDate}}", DateTime.Today.ToString("MMM dd, yyyy"))
                    .Replace("{{Location}}", "Campus Grounds")
                    .Replace("{{Infraction}}", "Major Conduct Violation")
                    .Replace("{{OfficerName}}", "Guidance Counselor");

                var lines = sample.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1 && lines[0].StartsWith("OFFICIAL", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Join(" ", lines.Skip(1).Take(4));
                }
                return string.Join(" ", lines.Take(4));
            }
        }

        public string RealisticDate => DateTime.Today.ToString("MMMM dd, yyyy");
        public string RealisticAddressee => "To the Parent / Legal Guardian of Juan Dela Cruz (ID: STD-1042):";
    }
}
