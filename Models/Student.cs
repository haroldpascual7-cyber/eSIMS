using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class Student : EntityBase
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string GradeLevel { get; set; } = "Grade 10";
        public string Section { get; set; } = string.Empty;
        public string TrackStrand { get; set; } = "Academic";
        public string Adviser { get; set; } = string.Empty;

        public string GuardianName { get; set; } = string.Empty;
        public string GuardianRelation { get; set; } = "Parent";
        public string GuardianContact { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;

        public string MedicalNotes { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;

        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {MiddleName[0]}. {LastName}".Trim();

        public string DisplayName => $"{FullName} ({StudentNumber})";
    }
}