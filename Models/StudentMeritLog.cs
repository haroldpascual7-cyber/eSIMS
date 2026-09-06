using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class StudentMeritLog : EntityBase
    {
        public string MeritId { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string CommendationType { get; set; } = "Peer Leadership";
        public int PointsAwarded { get; set; } = 5;
        public DateTime DateAwarded { get; set; } = DateTime.Now;
        public string AwardedBy { get; set; } = string.Empty;
        public string Citation { get; set; } = string.Empty;
    }
}