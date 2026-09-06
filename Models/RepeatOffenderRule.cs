using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class RepeatOffenderRule : EntityBase
    {
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public string Category { get; set; } = "Cumulative Infractions"; // All/Cumulative Infractions, Violence & Altercation, Bullying & Harassment, Vandalism & Property, Substance & Contraband, Attendance & Truancy
        public int LookbackDays { get; set; } = 30; // 15, 30, 45, 60, 90
        public int ThresholdCount { get; set; } = 2; // 2 to 10
        public string PenaltyType { get; set; } = "Add Flat Risk Points"; // Add Flat Risk Points, Bump Severity Tier by +1, Force Critical Tier Immediately
        public int BonusPoints { get; set; } = 25; // 0 to 50
        public bool AutoSummons { get; set; } = true;
        public bool FlagProfile { get; set; } = true;
        public bool RequireDeanSignoff { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Display Helpers
        public string LookbackText => $"{LookbackDays} Days";
        public string ThresholdText => $">= {ThresholdCount} Previous";
        public string PenaltyActionText => PenaltyType == "Add Flat Risk Points" ? $"+{BonusPoints} Risk Points" : PenaltyType;
    }
}