using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class UserManagementViewModel
    {
        private bool _isUserModalOpen;
        private bool _isModalEditing;
        private bool _isCreatingUser;
        private string _selectedRoleFilter = "All Roles";

        public ObservableCollection<string> RoleFilterOptions { get; } = new()
        {
            "All Roles", "Administrator", "Guidance Counselor", "Discipline Officer", "Staff"
        };

        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (SetProperty(ref _selectedRoleFilter, value))
                {
                    UsersView.Refresh();
                }
            }
        }

        public bool IsUserModalOpen
        {
            get => _isUserModalOpen;
            set => SetProperty(ref _isUserModalOpen, value);
        }

        public bool IsModalEditing
        {
            get => _isModalEditing;
            set => SetProperty(ref _isModalEditing, value);
        }

        public bool IsCreatingUser
        {
            get => _isCreatingUser;
            set => SetProperty(ref _isCreatingUser, value);
        }

        public string ModalTitle => IsCreatingUser ? "➕ Create New User Account" : (IsModalEditing ? "✏️ Edit User Account" : "👤 User Account Details");

        public ICommand OpenUserCardCommand => new RelayCommand<SystemUserModel>(user =>
        {
            if (user == null) return;
            SelectedUser = user;
            IsCreatingUser = false;
            IsModalEditing = false;
            IsUserModalOpen = true;
            OnPropertyChanged(nameof(ModalTitle));
        });

        public ICommand OpenCreateUserModalCommand => new RelayCommand(() =>
        {
            SelectedUser = null;
            ClearForm();
            IsCreatingUser = true;
            IsModalEditing = true;
            IsUserModalOpen = true;
            OnPropertyChanged(nameof(ModalTitle));
        });

        public ICommand CloseUserModalCommand => new RelayCommand(() =>
        {
            IsUserModalOpen = false;
            IsModalEditing = false;
            IsCreatingUser = false;
        });

        public ICommand SwitchToEditModeCommand => new RelayCommand(() =>
        {
            IsModalEditing = true;
            OnPropertyChanged(nameof(ModalTitle));
        });

        public ICommand CancelEditModeCommand => new RelayCommand(() =>
        {
            if (IsCreatingUser)
            {
                IsUserModalOpen = false;
            }
            else
            {
                IsModalEditing = false;
                OnSelectedUserChanged();
                OnPropertyChanged(nameof(ModalTitle));
            }
        });

        public ICommand SaveFromModalCommand => new RelayCommand(async () =>
        {
            if (IsCreatingUser)
            {
                await CreateUserAsync();
                if (string.IsNullOrWhiteSpace(NewUsername))
                {
                    IsUserModalOpen = false;
                }
            }
            else
            {
                await SaveUserAsync();
                IsModalEditing = false;
                OnPropertyChanged(nameof(ModalTitle));
            }
        });

        public ICommand DeleteFromModalCommand => new RelayCommand(async () =>
        {
            await DeleteUserAsync();
            if (SelectedUser == null)
            {
                IsUserModalOpen = false;
            }
        });

        public ICommand ToggleStatusFromModalCommand => new RelayCommand(async () =>
        {
            if (SelectedUser != null)
            {
                await ToggleStatusAsync(SelectedUser);
                OnSelectedUserChanged();
            }
        });
    }
}
