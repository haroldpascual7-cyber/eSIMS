using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class UserManagementHubViewModel : ObservableObject
    {
        private readonly Action _onUserCredentials;
        private readonly Action _onRbac;
        private readonly Action _onStaffRoster;

        public UserManagementHubViewModel(Action onUserCredentials, Action onRbac, Action onStaffRoster)
        {
            _onUserCredentials = onUserCredentials ?? throw new ArgumentNullException(nameof(onUserCredentials));
            _onRbac = onRbac ?? throw new ArgumentNullException(nameof(onRbac));
            _onStaffRoster = onStaffRoster ?? throw new ArgumentNullException(nameof(onStaffRoster));

            Title = "User & Access Management Hub";
            Subtitle = "Configure user accounts, role-based security permissions matrix, and institutional staff directory synchronization.";

            OpenUserCredentialsCommand = new RelayCommand(_ => _onUserCredentials());
            OpenRbacCommand = new RelayCommand(_ => _onRbac());
            OpenStaffRosterCommand = new RelayCommand(_ => _onStaffRoster());
        }

        public string Title { get; }
        public string Subtitle { get; }

        public ICommand OpenUserCredentialsCommand { get; }
        public ICommand OpenRbacCommand { get; }
        public ICommand OpenStaffRosterCommand { get; }
    }
}
