using System;
using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class TranscriptEntryModel
    {
        public DateTime EntryDate { get; set; }
        public string FormattedDate => EntryDate.ToString("MMM dd, yyyy");
        public string SourceModule { get; set; } = string.Empty;
        public string ReferenceCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ScoreImpact { get; set; }
        public string ScoreImpactText => ScoreImpact > 0 ? $"+{ScoreImpact} Pts" : ScoreImpact < 0 ? $"{ScoreImpact} Pts" : "0 Pts";
        public string HandledBy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = "Completed";

        public string EntryIcon => SourceModule switch
        {
            "Incident"     => "⚠️",
            "Consequence"  => "📋",
            "Restorative"  => "🤝",
            "Suspension"   => "🚫",
            "Merit Award"  => "🎖️",
            _              => "📄"
        };
    }

    public class StudentTranscriptResult
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string GradeLevel { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string TrackStrand { get; set; } = string.Empty;
        public string Adviser { get; set; } = string.Empty;

        public string GuardianName { get; set; } = string.Empty;
        public string GuardianRelation { get; set; } = string.Empty;
        public string GuardianContact { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;

        public string MedicalNotes { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;

        public List<TranscriptEntryModel> Entries { get; set; } = new();

        public int ConductScore { get; set; } = 100;
        public bool IsEligible { get; set; } = true;
        public string StandingLabel => IsEligible ? "GOOD STANDING" : "UNDER OBSERVATION";
        public string StandingColor => IsEligible ? "#059669" : "#DC2626";

        public int IncidentCount { get; set; }
        public int SanctionCount { get; set; }
        public int InterventionCount { get; set; }
        public int ActiveSuspensionCount { get; set; }
    }
}