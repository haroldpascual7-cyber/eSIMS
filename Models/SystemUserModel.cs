using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    /// <summary>
    /// Model representing a row from the SystemUsers table.
    /// Implements ObservableObject for instantaneous UI responsiveness.
    /// </summary>
    public class SystemUserModel : ObservableObject
    {
        private int _userId;
        private string _username = string.Empty;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _role = string.Empty;
        private string _status = "Active";
        private DateTime _createdAt;
        private string? _profilePicturePath;

        public int UserId { get => _userId; set => SetProperty(ref _userId, value); }
        public string Username { get => _username; set => SetProperty(ref _username, value); }
        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Role { get => _role; set => SetProperty(ref _role, value); }
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(ToggleLabel));
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }
        public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
        public string? ProfilePicturePath { get => _profilePicturePath; set => SetProperty(ref _profilePicturePath, value); }

        public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
        public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy");
        public string ToggleLabel => IsActive ? "⛔ Suspend" : "✅ Activate";
    }
}
