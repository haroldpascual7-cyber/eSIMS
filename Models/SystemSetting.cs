namespace SmartSchoolIncidentManagementSystem.Models
{
    public class SystemSetting : EntityBase
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
