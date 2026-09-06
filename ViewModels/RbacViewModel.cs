using Microsoft.Data.Sqlite;
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
    public class RbacViewModel : ObservableObject
    {
        private readonly Action _onBack;

        public ObservableCollection<RolePermissionModel> Permissions { get; } = new();

        public ICommand SavePermissionsCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand BackToHubCommand { get; }

        public RbacViewModel(Action onBack)
        {
            _onBack = onBack ?? throw new ArgumentNullException(nameof(onBack));

            SavePermissionsCommand = new RelayCommand(async _ => await SavePermissionsAsync());
            ResetDefaultsCommand = new RelayCommand(async _ => await ResetDefaultsAsync());
            BackToHubCommand = new RelayCommand(_ => _onBack());

            DatabaseInitializer.InitializeRolePermissions();
            _ = LoadPermissionsAsync();
        }

        private async Task LoadPermissionsAsync()
        {
            var list = new List<RolePermissionModel>();

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT PermissionId, ModuleName, AdminAccess, DisciplineOfficerAccess, GuidanceCounselorAccess, TeacherAccess, StaffAccess
                    FROM RolePermissions
                    ORDER BY PermissionId ASC;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new RolePermissionModel
                    {
                        PermissionId            = reader.GetInt32(0),
                        ModuleName              = reader.GetString(1),
                        AdminAccess             = reader.GetInt32(2) == 1,
                        DisciplineOfficerAccess = reader.GetInt32(3) == 1,
                        GuidanceCounselorAccess = reader.GetInt32(4) == 1,
                        TeacherAccess           = reader.GetInt32(5) == 1,
                        StaffAccess             = reader.GetInt32(6) == 1
                    });
                }
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                Permissions.Clear();
                foreach (var p in list)
                    Permissions.Add(p);
            });
        }

        private async Task SavePermissionsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                    conn.Open();

                    foreach (var p in Permissions)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            UPDATE RolePermissions
                            SET AdminAccess = @Adm,
                                DisciplineOfficerAccess = @Disc,
                                GuidanceCounselorAccess = @Guid,
                                TeacherAccess = @Teach,
                                StaffAccess = @Stf,
                                UpdatedAt = CURRENT_TIMESTAMP
                            WHERE PermissionId = @Id;";
                        cmd.Parameters.AddWithValue("@Adm", p.AdminAccess ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Disc", p.DisciplineOfficerAccess ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Guid", p.GuidanceCounselorAccess ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Teach", p.TeacherAccess ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Stf", p.StaffAccess ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Id", p.PermissionId);
                        cmd.ExecuteNonQuery();
                    }
                });

                await AuditLogger.LogAsync("ADMIN", "UPDATE", "RBAC", "Updated module-level role permissions matrix.");

                SessionManager.NotifyPermissionsChanged();

                MessageBox.Show("Permissions Matrix saved successfully!",
                    "RBAC Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving permissions: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ResetDefaultsAsync()
        {
            var confirm = MessageBox.Show("Reset permission matrix to institutional default values?",
                "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM RolePermissions;";
                cmd.ExecuteNonQuery();

                DatabaseInitializer.InitializeRolePermissions();
            });

            await LoadPermissionsAsync();
            await AuditLogger.LogAsync("ADMIN", "UPDATE", "RBAC", "Reset permission matrix to institutional defaults.");

            SessionManager.NotifyPermissionsChanged();

            MessageBox.Show("Permissions reset to defaults.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
