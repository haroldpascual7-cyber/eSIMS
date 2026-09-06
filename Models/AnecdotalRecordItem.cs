using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class AnecdotalRecordItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeSection { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime ObservationDate { get; set; } = DateTime.Now;
        public string FormattedDate => ObservationDate.ToString("MMM dd, yyyy • h:mm tt");
        public string SettingOrSubject { get; set; } = "Classroom / Homeroom";
        public string ObservationType { get; set; } = "Positive Conduct"; // Positive Conduct, Minor Infraction, Academic Focus, Attendance, Social/Peer Interaction
        public string NarrativeObservation { get; set; } = string.Empty;
        public string ActionTakenByTeacher { get; set; } = string.Empty;
        public string FollowUpPlan { get; set; } = "Monitor in class"; // Monitor in class, Notify Homeroom Adviser, Parent Check-in, Refer to Guidance
        public string Status { get; set; } = "Resolved in Class"; // Resolved in Class, Monitoring, Referred to Guidance

        public string TypeBadgeColor => ObservationType switch
        {
            "Positive Conduct" => "#10B981", // Emerald Green
            "Academic Focus" => "#0284C7", // Sky Blue
            "Social/Peer Interaction" => "#8B5CF6", // Purple
            "Attendance" => "#F59E0B", // Amber
            _ => "#EF4444" // Crimson Red for Minor Infraction
        };

        public string TypeBadgeBg => ObservationType switch
        {
            "Positive Conduct" => "#ECFDF5",
            "Academic Focus" => "#E0F2FE",
            "Social/Peer Interaction" => "#F3E8FF",
            "Attendance" => "#FEF3C7",
            _ => "#FEE2E2"
        };
    }
}
