using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Helpers;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class StaffRosterViewModel
    {
        // ── Filter state ──────────────────────────────────────────────────────
        private string _selectedDepartmentFilter = "All Departments";

        public ObservableCollection<string> DepartmentFilterOptions { get; } = new()
        {
            "All Departments",
            "Administration",
            "Discipline",
            "Guidance",
            "Academics",
            "Office Staff",
            "Science",
            "Mathematics"
        };

        public string SelectedDepartmentFilter
        {
            get => _selectedDepartmentFilter;
            set
            {
                if (SetProperty(ref _selectedDepartmentFilter, value))
                    StaffView.Refresh();
            }
        }

        // ── Modal state ───────────────────────────────────────────────────────
        private bool _isStaffModalOpen;
        private bool _isStaffModalEditing;
        private bool _isCreatingStaff;
        private StaffRosterModel? _viewingStaff;

        // Edit fields
        private string _editStaffNumber = string.Empty;
        private string _editFullName    = string.Empty;
        private string _editDepartment  = string.Empty;
        private string _editRole        = string.Empty;
        private string _editEmail       = string.Empty;
        private string _editPhone       = string.Empty;

        public bool IsStaffModalOpen
        {
            get => _isStaffModalOpen;
            set => SetProperty(ref _isStaffModalOpen, value);
        }

        public bool IsStaffModalEditing
        {
            get => _isStaffModalEditing;
            set => SetProperty(ref _isStaffModalEditing, value);
        }

        public StaffRosterModel? ViewingStaff
        {
            get => _viewingStaff;
            set => SetProperty(ref _viewingStaff, value);
        }

        public string StaffModalTitle => _isCreatingStaff ? "Add New Staff Member" : "Staff Details";

        public string EditStaffNumber { get => _editStaffNumber; set => SetProperty(ref _editStaffNumber, value); }
        public string EditFullName    { get => _editFullName;    set => SetProperty(ref _editFullName, value); }
        public string EditDepartment  { get => _editDepartment;  set => SetProperty(ref _editDepartment, value); }
        public string EditRole        { get => _editRole;        set => SetProperty(ref _editRole, value); }
        public string EditEmail       { get => _editEmail;       set => SetProperty(ref _editEmail, value); }
        public string EditPhone       { get => _editPhone;       set => SetProperty(ref _editPhone, value); }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand OpenStaffCardCommand        { get; private set; } = null!;
        public ICommand OpenCreateStaffModalCommand { get; private set; } = null!;
        public ICommand CloseStaffModalCommand      { get; private set; } = null!;
        public ICommand ToggleStaffStatusCommand    { get; private set; } = null!;
        public ICommand SwitchToStaffEditModeCommand{ get; private set; } = null!;
        public ICommand CancelStaffEditCommand      { get; private set; } = null!;
        public ICommand SaveStaffFromModalCommand   { get; private set; } = null!;

        private void InitModalCommands()
        {
            OpenStaffCardCommand         = new RelayCommand<StaffRosterModel>(OpenStaffCard);
            OpenCreateStaffModalCommand  = new RelayCommand(_ => OpenCreateStaff());
            CloseStaffModalCommand       = new RelayCommand(_ => IsStaffModalOpen = false);
            ToggleStaffStatusCommand     = new RelayCommand(async _ => await ToggleStaffStatusFromModalAsync());
            SwitchToStaffEditModeCommand = new RelayCommand(_ => EnterEditMode());
            CancelStaffEditCommand       = new RelayCommand(_ =>
            {
                if (_isCreatingStaff) IsStaffModalOpen = false;
                else IsStaffModalEditing = false;
            });
            SaveStaffFromModalCommand    = new RelayCommand(async _ => await SaveStaffFromModalAsync());
        }

        private void OpenStaffCard(StaffRosterModel? staff)
        {
            if (staff == null) return;
            ViewingStaff = staff;
            _isCreatingStaff = false;
            IsStaffModalEditing = false;
            IsStaffModalOpen = true;
            OnPropertyChanged(nameof(StaffModalTitle));
        }

        private void OpenCreateStaff()
        {
            ViewingStaff = null;
            _isCreatingStaff = true;
            EditStaffNumber = string.Empty;
            EditFullName    = string.Empty;
            EditDepartment  = string.Empty;
            EditRole        = string.Empty;
            EditEmail       = string.Empty;
            EditPhone       = string.Empty;
            IsStaffModalEditing = true;
            IsStaffModalOpen    = true;
            OnPropertyChanged(nameof(StaffModalTitle));
        }

        private void EnterEditMode()
        {
            if (ViewingStaff == null) return;
            EditStaffNumber = ViewingStaff.StaffNumber;
            EditFullName    = ViewingStaff.FullName;
            EditDepartment  = ViewingStaff.Department;
            EditRole        = ViewingStaff.AssignmentRole;
            EditEmail       = ViewingStaff.Email;
            EditPhone       = ViewingStaff.Phone;
            _isCreatingStaff    = false;
            IsStaffModalEditing = true;
            OnPropertyChanged(nameof(StaffModalTitle));
        }

        private async Task ToggleStaffStatusFromModalAsync()
        {
            if (ViewingStaff == null) return;

            var newStatus = ViewingStaff.Status == "Active" ? "Inactive" : "Active";

            await Task.Run(() =>
            {
                using var conn = DatabaseProvider.CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE StaffRoster SET Status = @Status WHERE StaffId = @Id;";
                cmd.AddParam("@Status", newStatus);
                cmd.AddParam("@Id", ViewingStaff.StaffId);
                cmd.ExecuteNonQuery();
            });

            await AuditLogger.LogAsync("ADMIN", "UPDATE", "StaffRoster",
                $"Changed status of '{ViewingStaff.FullName}' to '{newStatus}'.");

            MessageBox.Show($"'{ViewingStaff.FullName}' is now {newStatus}.", "Status Changed",
                MessageBoxButton.OK, MessageBoxImage.Information);

            IsStaffModalOpen = false;
            await LoadStaffAsync();
        }

        private async Task SaveStaffFromModalAsync()
        {
            if (string.IsNullOrWhiteSpace(EditStaffNumber)
             || string.IsNullOrWhiteSpace(EditFullName)
             || string.IsNullOrWhiteSpace(EditDepartment)
             || string.IsNullOrWhiteSpace(EditRole)
             || string.IsNullOrWhiteSpace(EditEmail))
            {
                MessageBox.Show("Please fill in all required fields (ID, Name, Department, Role, Email).",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isCreatingStaff)
                {
                    await Task.Run(() =>
                    {
                        using var conn = DatabaseProvider.CreateConnection();
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            INSERT INTO StaffRoster (StaffNumber, FullName, Department, AssignmentRole, Email, Phone, Status, CreatedAt)
                            VALUES (@Num, @Name, @Dept, @Role, @Email, @Phone, 'Active', CURRENT_TIMESTAMP);";
                        cmd.AddParam("@Num",   EditStaffNumber.Trim());
                        cmd.AddParam("@Name",  EditFullName.Trim());
                        cmd.AddParam("@Dept",  EditDepartment);
                        cmd.AddParam("@Role",  EditRole);
                        cmd.AddParam("@Email", EditEmail.Trim());
                        cmd.AddParam("@Phone", EditPhone.Trim());
                        cmd.ExecuteNonQuery();
                    });

                    await AuditLogger.LogAsync("ADMIN", "CREATE", "StaffRoster",
                        $"Added new staff member '{EditFullName.Trim()}' ({EditStaffNumber.Trim()}).");

                    MessageBox.Show($"Staff member '{EditFullName.Trim()}' added successfully!", "Staff Added",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (ViewingStaff == null) return;

                    await Task.Run(() =>
                    {
                        using var conn = DatabaseProvider.CreateConnection();
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            UPDATE StaffRoster
                            SET FullName = @Name, Department = @Dept, AssignmentRole = @Role, Email = @Email, Phone = @Phone
                            WHERE StaffId = @Id;";
                        cmd.AddParam("@Name",  EditFullName.Trim());
                        cmd.AddParam("@Dept",  EditDepartment);
                        cmd.AddParam("@Role",  EditRole);
                        cmd.AddParam("@Email", EditEmail.Trim());
                        cmd.AddParam("@Phone", EditPhone.Trim());
                        cmd.AddParam("@Id",    ViewingStaff.StaffId);
                        cmd.ExecuteNonQuery();
                    });

                    await AuditLogger.LogAsync("ADMIN", "UPDATE", "StaffRoster",
                        $"Updated record for staff member '{ViewingStaff.FullName}'.");

                    MessageBox.Show($"Changes for '{ViewingStaff.FullName}' saved!", "Staff Updated",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsStaffModalOpen = false;
                await LoadStaffAsync();
            }
            catch (System.Data.Common.DbException ex)
                when (ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                   || ex.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Staff Number '{EditStaffNumber.Trim()}' already exists.", "Duplicate Staff ID",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving staff: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
