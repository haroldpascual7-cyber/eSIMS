using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class LetterTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateBody { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}