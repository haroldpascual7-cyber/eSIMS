namespace SmartSchoolIncidentManagementSystem.Models
{
    public class User : EntityBase
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // e.g., Admin, Faculty
        public bool IsActive { get; set; } = true;
    }
}
