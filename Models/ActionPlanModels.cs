using System;
using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class IndividualActionPlan
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeSection { get; set; } = "Grade 10 - St. Thomas";
        public string FocusArea { get; set; } = string.Empty;
        public string AssignedCounselor { get; set; } = string.Empty;
        public string RiskTier { get; set; } = "Tier 2 - Moderate";
        public string StartDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string ReviewDate { get; set; } = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
        public string ProblemStatement { get; set; } = string.Empty;
        public string CoreRoadmapObjectives { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Under Evaluation, Closed
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<IapMilestone> Milestones { get; set; } = new();
        public List<IapCounselorNote> Notes { get; set; } = new();
        public List<IapAttachment> Attachments { get; set; } = new();

        public int CompletedMilestoneCount => Milestones?.FindAll(m => m.IsCompleted).Count ?? 0;
        public int TotalMilestoneCount => Milestones?.Count ?? 0;
        public double MilestoneProgressPercent => TotalMilestoneCount > 0 ? (double)CompletedMilestoneCount / TotalMilestoneCount * 100.0 : 0.0;
        public string ProgressText => $"{CompletedMilestoneCount} / {TotalMilestoneCount} Milestones";
        public string StatusBadgeColor => Status switch
        {
            "Active" => "#059669",
            "Under Evaluation" => "#D97706",
            "Closed" => "#2563EB",
            _ => "#64748B"
        };
    }

    public class IapMilestone
    {
        public string MilestoneId { get; set; } = Guid.NewGuid().ToString();
        public string PlanId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DueDate { get; set; } = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
        public bool IsCompleted { get; set; } = false;
    }

    public class IapCounselorNote
    {
        public string NoteId { get; set; } = Guid.NewGuid().ToString();
        public string PlanId { get; set; } = string.Empty;
        public string Author { get; set; } = "Guidance Counselor";
        public string NoteDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        public string Content { get; set; } = string.Empty;
    }

    public class IapAttachment
    {
        public string AttachmentId { get; set; } = Guid.NewGuid().ToString();
        public string PlanId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string UploadDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }
}