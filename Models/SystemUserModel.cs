using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    /// <summary>
    /// Model representing a row from the SystemUsers table.
    /// Used for DataGrid display in the User Management module.
    /// </summary>
    public class SystemUserModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Formatted creation date for DataGrid display.
        /// </summary>
        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy");

        /// <summary>
        /// Returns the toggle action label for the status button.
        /// </summary>
        public string ToggleLabel => Status == "Active" ? "⛔ Suspend" : "✅ Activate";
    }
}
