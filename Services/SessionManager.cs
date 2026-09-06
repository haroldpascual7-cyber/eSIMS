using System;
using System.Linq;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Static class holding current authenticated user session details and RBAC role checks.
    /// </summary>
    public static class SessionManager
    {
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
        public static string Email { get; set; } = string.Empty;
        public static bool IsAuthenticated { get; set; }

        public static void SetSession(string username, string fullName, string role, string email = "")
        {
            Username = username;
            FullName = string.IsNullOrWhiteSpace(fullName) ? username : fullName;
            Role = string.IsNullOrWhiteSpace(role) ? "System Administrator" : role;
            Email = email;
            IsAuthenticated = true;
        }

        public static event Action? PermissionsChanged;

        public static void NotifyPermissionsChanged()
        {
            PermissionsChanged?.Invoke();
        }

        public static bool HasPermission(string allowedRolesCommaSeparated)
        {
            if (!IsAuthenticated || string.IsNullOrWhiteSpace(Role))
                return false;

            if (string.IsNullOrWhiteSpace(allowedRolesCommaSeparated))
                return true;

            var currentRole = Role.Trim().ToLowerInvariant();
            if (currentRole.Contains("system administrator") || currentRole.Contains("admin"))
                return true;

            var roles = allowedRolesCommaSeparated
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim().ToLowerInvariant());

            return roles.Any(r => currentRole.Contains(r) || r.Contains(currentRole));
        }

        public static bool HasModuleAccess(string moduleName, string defaultAllowedRoles = "")
        {
            if (!IsAuthenticated || string.IsNullOrWhiteSpace(Role))
                return false;

            var currentRole = Role.Trim();
            if (currentRole.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            try
            {
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseInitializer.GetConnectionString());
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT ModuleName, AdminAccess, DisciplineOfficerAccess, GuidanceCounselorAccess, TeacherAccess, StaffAccess
                    FROM RolePermissions;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dbModule = reader.GetString(0);
                    // Match full name or partial module keyword
                    if (dbModule.IndexOf(moduleName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        moduleName.IndexOf(dbModule, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        IsModuleMatch(dbModule, moduleName))
                    {
                        if (currentRole.IndexOf("Discipline", StringComparison.OrdinalIgnoreCase) >= 0)
                            return reader.GetInt32(2) == 1;
                        if (currentRole.IndexOf("Guidance", StringComparison.OrdinalIgnoreCase) >= 0 || currentRole.IndexOf("Counselor", StringComparison.OrdinalIgnoreCase) >= 0)
                            return reader.GetInt32(3) == 1;
                        if (currentRole.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0)
                            return reader.GetInt32(4) == 1;
                        if (currentRole.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0)
                            return reader.GetInt32(5) == 1;
                    }
                }
            }
            catch
            {
                // Fallback to default roles on database read exception
            }

            if (!string.IsNullOrWhiteSpace(defaultAllowedRoles))
            {
                return HasPermission(defaultAllowedRoles);
            }

            return false;
        }

        private static bool IsModuleMatch(string dbMod, string queryMod)
        {
            var db = dbMod.ToLowerInvariant();
            var q = queryMod.ToLowerInvariant();
            if (db.Contains("investigation") && q.Contains("investigation")) return true;
            if (db.Contains("parent") && (q.Contains("parent") || q.Contains("notice") || q.Contains("letter"))) return true;
            if (db.Contains("student") && (q.Contains("student") || q.Contains("profile") || q.Contains("transcript") || q.Contains("iap"))) return true;
            if (db.Contains("incident") && (q.Contains("incident") || q.Contains("intake"))) return true;
            if (db.Contains("disciplinary") && (q.Contains("disciplinary") || q.Contains("consequence") || q.Contains("suspension") || q.Contains("restorative") || q.Contains("risk") || q.Contains("rule"))) return true;
            if (db.Contains("report") && (q.Contains("report") || q.Contains("analytics") || q.Contains("heatmap") || q.Contains("recidivism"))) return true;
            if (db.Contains("audit") && (q.Contains("audit") || q.Contains("security") || q.Contains("integrity"))) return true;
            if (db.Contains("user") && (q.Contains("user") || q.Contains("rbac") || q.Contains("roster"))) return true;
            return false;
        }

        public static void Clear()
        {
            Username = string.Empty;
            FullName = string.Empty;
            Role = string.Empty;
            Email = string.Empty;
            IsAuthenticated = false;
        }
    }
}
