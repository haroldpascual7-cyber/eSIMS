using Microsoft.Data.Sqlite;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly Action _navigateBack;

        // ---------- Observable Collection & Filtered View ----------
        public ObservableCollection<SystemUserModel> Users { get; } = new();
        public ICollectionView UsersView { get; }

        // ---------- Selected User (Master-Detail) ----------
        private SystemUserModel? _selectedUser;
        private bool _isEditMode;
        private string _formHeader = "➕ Create New System Account";

        public SystemUserModel? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnSelectedUserChanged();
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string FormHeader
        {
            get => _formHeader;
            set => SetProperty(ref _formHeader, value);
        }

        // ---------- Online Admin Badge ----------
        public string OnlineAdminText => "admin (System Administrator)";

        // ---------- Form Fields ----------
        private string _newUsername = string.Empty;
        private string _newFullName = string.Empty;
        private string _newEmail = string.Empty;
        private string _newPassword = string.Empty;
        private string _selectedRole = string.Empty;
        private string _searchText = string.Empty;

        public string NewUsername
        {
            get => _newUsername;
            set => SetProperty(ref _newUsername, value);
        }

        public string NewFullName
        {
            get => _newFullName;
            set => SetProperty(ref _newFullName, value);
        }

        public string NewEmail
        {
            get => _newEmail;
            set => SetProperty(ref _newEmail, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChanged(nameof(HasSearchText));
                    UsersView.Refresh();
                }
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        // ---------- Available Roles ----------
        public List<string> AvailableRoles { get; } = new()
        {
            "Administrator",
            "Guidance Counselor",
            "Discipline Officer",
            "Staff"
        };

        // ---------- Commands ----------
        public ICommand CreateUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand ToggleStatusCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand BackToHubCommand { get; }

        // ---------- Constructor ----------
        public UserManagementViewModel(Action navigateBack)
        {
            _navigateBack = navigateBack ?? throw new ArgumentNullException(nameof(navigateBack));

            UsersView = CollectionViewSource.GetDefaultView(Users);
            UsersView.Filter = FilterUsers;

            CreateUserCommand = new RelayCommand(async _ => await CreateUserAsync(), _ => CanCreateUser());
            SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync(), _ => CanSaveUser());
            DeleteUserCommand = new RelayCommand(async _ => await DeleteUserAsync(), _ => IsEditMode && SelectedUser != null);
            ClearFormCommand = new RelayCommand(_ => ClearForm());
            ClearSelectionCommand = new RelayCommand(_ => SelectedUser = null);
            ToggleStatusCommand = new RelayCommand(async p => await ToggleStatusAsync(p as SystemUserModel ?? SelectedUser));
            ResetPasswordCommand = new RelayCommand(async p => await ResetPasswordAsync(p as SystemUserModel ?? SelectedUser));
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            BackToHubCommand = new RelayCommand(_ => _navigateBack());

            DatabaseInitializer.InitializeSystemUsers();
            _ = LoadUsersAsync();
        }

        // ---------- Selection Handler ----------
        private void OnSelectedUserChanged()
        {
            if (SelectedUser != null)
            {
                IsEditMode = true;
                FormHeader = $"✏️ Manage Account: {SelectedUser.Username}";
                NewUsername = SelectedUser.Username;
                NewFullName = SelectedUser.FullName;
                NewEmail = SelectedUser.Email;
                SelectedRole = SelectedUser.Role;
                NewPassword = string.Empty;
            }
            else
            {
                IsEditMode = false;
                FormHeader = "➕ Create New System Account";
                ClearForm();
            }
        }

        // ---------- Filter ----------
        private bool FilterUsers(object obj)
        {
            if (obj is not SystemUserModel user) return false;

            if (!string.IsNullOrWhiteSpace(SelectedRoleFilter) && SelectedRoleFilter != "All Roles" &&
                !user.Role.Equals(SelectedRoleFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var term = SearchText.Trim().ToLowerInvariant();
            return user.Username.ToLowerInvariant().Contains(term)
                || user.FullName.ToLowerInvariant().Contains(term)
                || user.Email.ToLowerInvariant().Contains(term)
                || user.Role.ToLowerInvariant().Contains(term)
                || user.Status.ToLowerInvariant().Contains(term);
        }

        // ---------- Load from SQLite ----------
        private async Task LoadUsersAsync()
        {
            var list = new List<SystemUserModel>();

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT UserId, Username, FullName, Email, Role, Status, CreatedAt, ProfilePicturePath
                    FROM SystemUsers
                    ORDER BY UserId ASC;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new SystemUserModel
                    {
                        UserId    = reader.GetInt32(0),
                        Username  = reader.GetString(1),
                        FullName  = reader.GetString(2),
                        Email     = reader.GetString(3),
                        Role      = reader.GetString(4),
                        Status    = reader.GetString(5),
                        CreatedAt = reader.IsDBNull(6) ? DateTime.Now : reader.GetDateTime(6),
                        ProfilePicturePath = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                var prevSelectedId = SelectedUser?.UserId;
                Users.Clear();
                SystemUserModel? reselectItem = null;
                foreach (var u in list)
                {
                    Users.Add(u);
                    if (prevSelectedId.HasValue && u.UserId == prevSelectedId.Value)
                        reselectItem = u;
                }
                UsersView.Refresh();
                if (reselectItem != null)
                {
                    SelectedUser = reselectItem;
                    SelectedUserPhotoPath = reselectItem.ProfilePicturePath;
                }
            });
        }

        // ---------- Create User ----------
        private async Task CreateUserAsync()
        {
            if (!CanCreateUser()) return;

            var hashedPassword = DatabaseInitializer.HashPassword(NewPassword);

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO SystemUsers (Username, FullName, Email, PasswordHash, Role, Status, CreatedAt)
                        VALUES (@Username, @FullName, @Email, @PasswordHash, @Role, 'Active', CURRENT_TIMESTAMP);";

                    cmd.Parameters.AddWithValue("@Username", NewUsername.Trim());
                    cmd.Parameters.AddWithValue("@FullName", NewFullName.Trim());
                    cmd.Parameters.AddWithValue("@Email", NewEmail.Trim());
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    cmd.Parameters.AddWithValue("@Role", SelectedRole);
                    cmd.ExecuteNonQuery();
                });

                await AuditLogger.LogAsync(
                    "ADMIN", "CREATE", "UserManagement",
                    $"Created new account '{NewUsername.Trim()}' with role '{SelectedRole}'.");

                MessageBox.Show($"Account '{NewUsername.Trim()}' created successfully!",
                    "Account Created", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                await LoadUsersAsync();
            }
            catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                MessageBox.Show($"Username '{NewUsername.Trim()}' already exists.",
                    "Duplicate Username", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating account: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Save Changes (Edit Mode) ----------
        private async Task SaveUserAsync()
        {
            if (!CanSaveUser() || SelectedUser == null) return;

            try
            {
                bool updatePassword = !string.IsNullOrWhiteSpace(NewPassword);
                var hashedPassword = updatePassword ? DatabaseInitializer.HashPassword(NewPassword) : string.Empty;

                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    if (updatePassword)
                    {
                        cmd.CommandText = @"
                            UPDATE SystemUsers
                            SET FullName = @FullName, Email = @Email, Role = @Role, PasswordHash = @PasswordHash
                            WHERE UserId = @UserId;";
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE SystemUsers
                            SET FullName = @FullName, Email = @Email, Role = @Role
                            WHERE UserId = @UserId;";
                    }

                    cmd.Parameters.AddWithValue("@FullName", NewFullName.Trim());
                    cmd.Parameters.AddWithValue("@Email", NewEmail.Trim());
                    cmd.Parameters.AddWithValue("@Role", SelectedRole);
                    cmd.Parameters.AddWithValue("@UserId", SelectedUser.UserId);
                    cmd.ExecuteNonQuery();
                });

                await AuditLogger.LogAsync(
                    "ADMIN", "UPDATE", "UserManagement",
                    $"Updated profile details for account '{SelectedUser.Username}'.");

                MessageBox.Show($"Changes for '{SelectedUser.Username}' saved successfully!",
                    "Account Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Delete / Terminate Account ----------
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to permanently terminate/delete the account '{SelectedUser.Username}'?\n\nThis action cannot be undone.",
                "Confirm Account Termination",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var targetUsername = SelectedUser.Username;
            var targetId = SelectedUser.UserId;

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM SystemUsers WHERE UserId = @UserId;";
                    cmd.Parameters.AddWithValue("@UserId", targetId);
                    cmd.ExecuteNonQuery();
                });

                await AuditLogger.LogAsync(
                    "ADMIN", "DELETE", "UserManagement",
                    $"Permanently deleted account '{targetUsername}'.");

                MessageBox.Show($"Account '{targetUsername}' has been terminated.",
                    "Account Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedUser = null;
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting account: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Toggle Status ----------
        private async Task ToggleStatusAsync(SystemUserModel? user)
        {
            if (user == null) return;

            var newStatus = user.Status == "Active" ? "Suspended" : "Active";
            var verb = newStatus == "Suspended" ? "suspend" : "reactivate";

            var confirm = MessageBox.Show(
                $"Are you sure you want to {verb} the account '{user.Username}'?",
                "Confirm Status Change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE SystemUsers SET Status = @Status WHERE UserId = @UserId;";
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@UserId", user.UserId);
                cmd.ExecuteNonQuery();
            });

            await AuditLogger.LogAsync(
                "ADMIN", "UPDATE", "UserManagement",
                $"Changed status of '{user.Username}' to '{newStatus}'.");

            MessageBox.Show($"User '{user.Username}' is now {newStatus}.",
                "Status Updated", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadUsersAsync();
        }

        // ---------- Reset Password ----------
        private async Task ResetPasswordAsync(SystemUserModel? user)
        {
            if (user == null) return;

            var confirm = MessageBox.Show(
                $"Reset the password for '{user.Username}'?\n\nA temporary password will be generated.",
                "Confirm Password Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var tempPassword = $"Temp{DateTime.Now:mmss}!";
            var hashedTemp = DatabaseInitializer.HashPassword(tempPassword);

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE SystemUsers SET PasswordHash = @Hash WHERE UserId = @UserId;";
                cmd.Parameters.AddWithValue("@Hash", hashedTemp);
                cmd.Parameters.AddWithValue("@UserId", user.UserId);
                cmd.ExecuteNonQuery();
            });

            await AuditLogger.LogAsync(
                "ADMIN", "UPDATE", "UserManagement",
                $"Reset password for user '{user.Username}'. Temporary password assigned.");

            MessageBox.Show(
                $"Password for '{user.Username}' has been reset.\n\n" +
                $"Temporary Password: {tempPassword}\n\n" +
                $"Please share this securely with the user.",
                "Password Reset Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await LoadUsersAsync();
        }
    }
}
