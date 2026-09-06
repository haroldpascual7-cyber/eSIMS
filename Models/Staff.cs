namespace SmartSchoolIncidentManagementSystem.Models
{
    public class Staff : EntityBase
    {
        public string StaffNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";
    }
}
