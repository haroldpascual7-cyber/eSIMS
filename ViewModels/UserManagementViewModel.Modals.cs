using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
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

        private string? _selectedUserPhotoPath;
        public string? SelectedUserPhotoPath
        {
            get => _selectedUserPhotoPath;
            set
            {
                if (SetProperty(ref _selectedUserPhotoPath, value))
                {
                    OnPropertyChanged(nameof(HasSelectedUserPhoto));
                }
            }
        }

        public bool HasSelectedUserPhoto => !string.IsNullOrWhiteSpace(SelectedUserPhotoPath) && System.IO.File.Exists(SelectedUserPhotoPath);

        public string ModalTitle => IsCreatingUser ? "➕ Create New User Account" : (IsModalEditing ? "✏️ Edit User Account" : "👤 User Account Details");

        public ICommand UploadPhotoCommand => new RelayCommand(async () =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Profile Photo",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var avatarsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserAvatars");
                    if (!System.IO.Directory.Exists(avatarsDir))
                    {
                        System.IO.Directory.CreateDirectory(avatarsDir);
                    }
                    var ext = System.IO.Path.GetExtension(dlg.FileName);
                    var username = SelectedUser?.Username ?? "user";
                    var destFile = System.IO.Path.Combine(avatarsDir, $"{username}_{DateTime.Now.Ticks}{ext}");
                    System.IO.File.Copy(dlg.FileName, destFile, true);

                    SelectedUserPhotoPath = destFile;

                    if (SelectedUser != null)
                    {
                        SelectedUser.ProfilePicturePath = destFile;
                        await Task.Run(() =>
                        {
                            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseInitializer.GetConnectionString());
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = "UPDATE SystemUsers SET ProfilePicturePath = @Path WHERE UserId = @UserId;";
                            cmd.Parameters.AddWithValue("@Path", destFile);
                            cmd.Parameters.AddWithValue("@UserId", SelectedUser.UserId);
                            cmd.ExecuteNonQuery();
                        });
                        UsersView.Refresh();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving profile photo: {ex.Message}", "Upload Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        });

        public ICommand RemovePhotoCommand => new RelayCommand(async () =>
        {
            SelectedUserPhotoPath = null;

            if (SelectedUser != null)
            {
                SelectedUser.ProfilePicturePath = null;
                await Task.Run(() =>
                {
                    using var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE SystemUsers SET ProfilePicturePath = NULL WHERE UserId = @UserId;";
                    cmd.Parameters.AddWithValue("@UserId", SelectedUser.UserId);
                    cmd.ExecuteNonQuery();
                });
                UsersView.Refresh();
            }
        });

        // ---------- Form Validation & Reset Helpers ----------
        private bool CanCreateUser()
        {
            return !IsEditMode
                && !string.IsNullOrWhiteSpace(NewUsername)
                && !string.IsNullOrWhiteSpace(NewFullName)
                && !string.IsNullOrWhiteSpace(NewEmail)
                && !string.IsNullOrWhiteSpace(NewPassword)
                && !string.IsNullOrWhiteSpace(SelectedRole);
        }

        private bool CanSaveUser()
        {
            return IsEditMode
                && SelectedUser != null
                && !string.IsNullOrWhiteSpace(NewFullName)
                && !string.IsNullOrWhiteSpace(NewEmail)
                && !string.IsNullOrWhiteSpace(SelectedRole);
        }

        private void ClearForm()
        {
            NewUsername = string.Empty;
            NewFullName = string.Empty;
            NewEmail = string.Empty;
            NewPassword = string.Empty;
            SelectedRole = string.Empty;
            SelectedUserPhotoPath = null;
        }

        public ICommand OpenUserCardCommand => new RelayCommand<SystemUserModel>(user =>
        {
            if (user == null) return;
            SelectedUser = user;
            SelectedUserPhotoPath = user.ProfilePicturePath;
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
            if (SelectedUser != null)
            {
                NewUsername = SelectedUser.Username;
                NewFullName = SelectedUser.FullName;
                NewEmail = SelectedUser.Email;
                SelectedRole = SelectedUser.Role;
                NewPassword = string.Empty;
            }
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
