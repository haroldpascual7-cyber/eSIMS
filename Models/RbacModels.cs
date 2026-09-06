namespace SmartSchoolIncidentManagementSystem.Models
{
    public class RolePermissionModel
    {
        public int PermissionId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public bool AdminAccess { get; set; } = true;
        public bool DisciplineOfficerAccess { get; set; } = true;
        public bool GuidanceCounselorAccess { get; set; } = true;
        public bool TeacherAccess { get; set; } = true;
        public bool StaffAccess { get; set; } = true;
    }
}
