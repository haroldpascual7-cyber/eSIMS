using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class ProbationProgressNote : EntityBase
    {
        public int ProbationRecordId { get; set; }
        public ProbationRecord? ProbationRecord { get; set; }
        public DateTime NoteDate { get; set; } = DateTime.Now;
        public string ProgressStatus { get; set; } = "On Track";
        public string Notes { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;

        public string NoteDateText => NoteDate.ToString("MMM dd, yyyy h:mm tt");
    }
}
