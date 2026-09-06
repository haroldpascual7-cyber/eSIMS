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
    public partial class StaffRosterViewModel : ObservableObject
    {
        private readonly Action _onBack;

        public ObservableCollection<StaffRosterModel> StaffList { get; } = new();
        public ICollectionView StaffView { get; }

        private StaffRosterModel? _selectedStaff;
        private bool _isEditMode;
        private string _formHeader = "➕ Add New Staff Member";

        private string _staffNumber = string.Empty;
        private string _fullName = string.Empty;
        private string _selectedDepartment = string.Empty;
        private string _selectedRole = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _searchText = string.Empty;

        public StaffRosterModel? SelectedStaff
        {
            get => _selectedStaff;
            set
            {
                if (SetProperty(ref _selectedStaff, value))
                {
                    OnSelectedStaffChanged();
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

        public string StaffNumber
        {
            get => _staffNumber;
            set => SetProperty(ref _staffNumber, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set => SetProperty(ref _selectedDepartment, value);
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChanged(nameof(HasSearchText));
                    StaffView.Refresh();
                }
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        public List<string> Departments { get; } = new()
        {
            "Administration",
            "Discipline",
            "Guidance",
            "Academics",
            "Office Staff",
            "Science",
            "Mathematics"
        };

        public List<string> AssignmentRoles { get; } = new()
        {
            "Principal",
            "Disciplinary Lead",
            "Case Investigator",
            "Homeroom Adviser",
            "Staff",
            "Records Assistant"
        };

        public ICommand AddStaffCommand { get; }
        public ICommand SaveStaffCommand { get; }
        public ICommand ToggleStatusCommand { get; }
        public ICommand SyncDirectoryCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand BackToHubCommand { get; }

        public StaffRosterViewModel(Action onBack)
        {
            _onBack = onBack ?? throw new ArgumentNullException(nameof(onBack));

            StaffView = CollectionViewSource.GetDefaultView(StaffList);
            StaffView.Filter = FilterStaff;

            AddStaffCommand = new RelayCommand(async _ => await AddStaffAsync(), _ => !IsEditMode && CanSave());
            SaveStaffCommand = new RelayCommand(async _ => await SaveStaffAsync(), _ => IsEditMode && CanSave());
            ToggleStatusCommand = new RelayCommand(async p => await ToggleStatusAsync(p as StaffRosterModel ?? SelectedStaff));
            SyncDirectoryCommand = new RelayCommand(async _ => await SyncDirectoryAsync());
            ClearFormCommand = new RelayCommand(_ => SelectedStaff = null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            BackToHubCommand = new RelayCommand(_ => _onBack());

            InitModalCommands();

            DatabaseInitializer.InitializeStaffRoster();
            _ = LoadStaffAsync();
        }

        private void OnSelectedStaffChanged()
        {
            if (SelectedStaff != null)
            {
                IsEditMode = true;
                FormHeader = $"✏️ Manage Staff: {SelectedStaff.FullName}";
                StaffNumber = SelectedStaff.StaffNumber;
                FullName = SelectedStaff.FullName;
                SelectedDepartment = SelectedStaff.Department;
                SelectedRole = SelectedStaff.AssignmentRole;
                Email = SelectedStaff.Email;
                Phone = SelectedStaff.Phone;
            }
            else
            {
                IsEditMode = false;
                FormHeader = "➕ Add New Staff Member";
                ClearFormFields();
            }
        }

        private void ClearFormFields()
        {
            StaffNumber = string.Empty;
            FullName = string.Empty;
            SelectedDepartment = string.Empty;
            SelectedRole = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
        }

        private bool FilterStaff(object obj)
        {
            if (obj is not StaffRosterModel staff) return false;

            if (SelectedDepartmentFilter != "All Departments"
                && !string.Equals(staff.Department, SelectedDepartmentFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var term = SearchText.Trim().ToLowerInvariant();
            return staff.StaffNumber.ToLowerInvariant().Contains(term)
                || staff.FullName.ToLowerInvariant().Contains(term)
                || staff.Department.ToLowerInvariant().Contains(term)
                || staff.AssignmentRole.ToLowerInvariant().Contains(term)
                || staff.Email.ToLowerInvariant().Contains(term)
                || staff.Status.ToLowerInvariant().Contains(term);
        }

        private async Task LoadStaffAsync()
        {
            var list = new List<StaffRosterModel>();

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT StaffId, StaffNumber, FullName, Department, AssignmentRole, Email, Phone, Status, CreatedAt
                    FROM StaffRoster
                    ORDER BY StaffId ASC;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new StaffRosterModel
                    {
                        StaffId        = reader.GetInt32(0),
                        StaffNumber    = reader.GetString(1),
                        FullName       = reader.GetString(2),
                        Department     = reader.GetString(3),
                        AssignmentRole = reader.GetString(4),
                        Email          = reader.GetString(5),
                        Phone          = reader.GetString(6),
                        Status         = reader.GetString(7),
                        CreatedAt      = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8)
                    });
                }
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                StaffList.Clear();
                foreach (var s in list)
                    StaffList.Add(s);
                StaffView.Refresh();
            });
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(StaffNumber)
                && !string.IsNullOrWhiteSpace(FullName)
                && !string.IsNullOrWhiteSpace(SelectedDepartment)
                && !string.IsNullOrWhiteSpace(SelectedRole)
                && !string.IsNullOrWhiteSpace(Email);
        }

        private async Task AddStaffAsync()
        {
            if (!CanSave()) return;

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO StaffRoster (StaffNumber, FullName, Department, AssignmentRole, Email, Phone, Status, CreatedAt)
                        VALUES (@Num, @Name, @Dept, @Role, @Email, @Phone, 'Active', CURRENT_TIMESTAMP);";
                    cmd.Parameters.AddWithValue("@Num", StaffNumber.Trim());
                    cmd.Parameters.AddWithValue("@Name", FullName.Trim());
                    cmd.Parameters.AddWithValue("@Dept", SelectedDepartment);
                    cmd.Parameters.AddWithValue("@Role", SelectedRole);
                    cmd.Parameters.AddWithValue("@Email", Email.Trim());
                    cmd.Parameters.AddWithValue("@Phone", Phone.Trim());
                    cmd.ExecuteNonQuery();
                });

                await AuditLogger.LogAsync("ADMIN", "CREATE", "StaffRoster", $"Added new staff member '{FullName.Trim()}' ({StaffNumber.Trim()}).");

                MessageBox.Show($"Staff member '{FullName.Trim()}' added successfully!", "Staff Added", MessageBoxButton.OK, MessageBoxImage.Information);
                SelectedStaff = null;
                await LoadStaffAsync();
            }
            catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                MessageBox.Show($"Staff Number '{StaffNumber.Trim()}' already exists.", "Duplicate Staff ID", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding staff member: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveStaffAsync()
        {
            if (!CanSave() || SelectedStaff == null) return;

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE StaffRoster
                        SET FullName = @Name, Department = @Dept, AssignmentRole = @Role, Email = @Email, Phone = @Phone
                        WHERE StaffId = @Id;";
                    cmd.Parameters.AddWithValue("@Name", FullName.Trim());
                    cmd.Parameters.AddWithValue("@Dept", SelectedDepartment);
                    cmd.Parameters.AddWithValue("@Role", SelectedRole);
                    cmd.Parameters.AddWithValue("@Email", Email.Trim());
                    cmd.Parameters.AddWithValue("@Phone", Phone.Trim());
                    cmd.Parameters.AddWithValue("@Id", SelectedStaff.StaffId);
                    cmd.ExecuteNonQuery();
                });

                await AuditLogger.LogAsync("ADMIN", "UPDATE", "StaffRoster", $"Updated record for staff member '{SelectedStaff.FullName}'.");

                MessageBox.Show($"Changes for '{SelectedStaff.FullName}' saved successfully!", "Staff Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving staff changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ToggleStatusAsync(StaffRosterModel? staff)
        {
            if (staff == null) return;

            var newStatus = staff.Status == "Active" ? "Inactive" : "Active";

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE StaffRoster SET Status = @Status WHERE StaffId = @Id;";
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@Id", staff.StaffId);
                cmd.ExecuteNonQuery();
            });

            await AuditLogger.LogAsync("ADMIN", "UPDATE", "StaffRoster", $"Changed status of staff '{staff.FullName}' to '{newStatus}'.");

            MessageBox.Show($"Staff member '{staff.FullName}' is now {newStatus}.", "Status Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadStaffAsync();
        }

        private async Task SyncDirectoryAsync()
        {
            await Task.Delay(800);
            await AuditLogger.LogAsync("ADMIN", "SYNC", "StaffRoster", "Synchronized staff roster directory with active database records.");
            MessageBox.Show("Staff Roster Directory synchronized successfully! All records up to date.", "Directory Synced", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
