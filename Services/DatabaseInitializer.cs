using Microsoft.Data.Sqlite;
using SmartSchoolIncidentManagementSystem.Helpers;
using System;
using System.IO;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Static initializer for system database tables (SystemUsers, RolePermissions, StaffRoster).
    /// Creates schemas and seeds initial records if tables are empty.
    /// </summary>
    public static class DatabaseInitializer
    {
        private static string? _connectionString;

        public static string GetConnectionString()
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return _connectionString;

            string dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartSchool");
            Directory.CreateDirectory(dbFolder);

            string dbPath = Path.Combine(dbFolder, "incidents.db");
            _connectionString = $"Data Source={dbPath}";
            return _connectionString;
        }

        public static string HashPassword(string password)
        {
            return SecurityHelper.ComputeSha256Hash(password);
        }

        public static void InitializeAllTables()
        {
            InitializeSystemUsers();
            InitializeRolePermissions();
            InitializeStaffRoster();
        }

        public static void InitializeSystemUsers()
        {
            using var conn = new SqliteConnection(GetConnectionString());
            conn.Open();

            using var createCmd = conn.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS SystemUsers (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    FullName TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    Status TEXT DEFAULT 'Active',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ProfilePicturePath TEXT
                );";
            createCmd.ExecuteNonQuery();

            try
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE SystemUsers ADD COLUMN ProfilePicturePath TEXT;";
                alterCmd.ExecuteNonQuery();
            }
            catch { /* Column already exists */ }

            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM SystemUsers;";
            long count = (long)(countCmd.ExecuteScalar() ?? 0);

            if (count == 0)
            {
                SeedDefaultAccounts(conn);
            }
            else
            {
                EnsureFourDemoAccounts(conn);
            }
        }

        public static void InitializeRolePermissions()
        {
            using var conn = new SqliteConnection(GetConnectionString());
            conn.Open();

            using var createCmd = conn.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS RolePermissions (
                    PermissionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ModuleName TEXT UNIQUE NOT NULL,
                    AdminAccess INTEGER DEFAULT 1,
                    DisciplineOfficerAccess INTEGER DEFAULT 1,
                    GuidanceCounselorAccess INTEGER DEFAULT 1,
                    TeacherAccess INTEGER DEFAULT 1,
                    StaffAccess INTEGER DEFAULT 1,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            createCmd.ExecuteNonQuery();

            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM RolePermissions;";
            long count = (long)(countCmd.ExecuteScalar() ?? 0);

            if (count == 0)
            {
                var modules = new[]
                {
                    ("Incident Intake & Reporting",     1, 1, 1, 1, 1),
                    ("Investigations & Evidence",        1, 1, 1, 1, 1),
                    ("Student Profiles & IAPs",          1, 1, 1, 1, 1),
                    ("Parent Notices & Letters",        1, 1, 1, 1, 1),
                    ("Disciplinary Actions & Sanctions", 1, 1, 1, 0, 0),
                    ("Reports & Safety Analytics",      1, 1, 1, 1, 1),
                    ("Audit Trail & Security Logs",     1, 0, 0, 0, 0),
                    ("User Management & Settings",      1, 0, 0, 0, 0)
                };

                foreach (var (mod, adm, disc, guid, teach, stf) in modules)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO RolePermissions (ModuleName, AdminAccess, DisciplineOfficerAccess, GuidanceCounselorAccess, TeacherAccess, StaffAccess, UpdatedAt)
                        VALUES (@Mod, @Adm, @Disc, @Guid, @Teach, @Stf, CURRENT_TIMESTAMP);";
                    cmd.Parameters.AddWithValue("@Mod", mod);
                    cmd.Parameters.AddWithValue("@Adm", adm);
                    cmd.Parameters.AddWithValue("@Disc", disc);
                    cmd.Parameters.AddWithValue("@Guid", guid);
                    cmd.Parameters.AddWithValue("@Teach", teach);
                    cmd.Parameters.AddWithValue("@Stf", stf);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Ensure 'Investigations & Evidence' exists in existing database
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM RolePermissions WHERE ModuleName LIKE '%Investigation%';";
                long invCount = (long)(checkCmd.ExecuteScalar() ?? 0);
                if (invCount == 0)
                {
                    using var insCmd = conn.CreateCommand();
                    insCmd.CommandText = @"
                        INSERT INTO RolePermissions (ModuleName, AdminAccess, DisciplineOfficerAccess, GuidanceCounselorAccess, TeacherAccess, StaffAccess, UpdatedAt)
                        VALUES ('Investigations & Evidence', 1, 1, 1, 1, 1, CURRENT_TIMESTAMP);";
                    insCmd.ExecuteNonQuery();
                }

                // Synchronize default Teacher and Staff access so both roles have the exact same permissions
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE RolePermissions 
                    SET TeacherAccess = 1, StaffAccess = 1 
                    WHERE ModuleName IN ('Parent Notices & Letters', 'Student Profiles & IAPs', 'Investigations & Evidence', 'Reports & Safety Analytics', 'Incident Intake & Reporting')
                      AND (TeacherAccess = 0 OR StaffAccess = 0);
                    UPDATE RolePermissions
                    SET GuidanceCounselorAccess = 1
                    WHERE ModuleName = 'Disciplinary Actions & Sanctions';";
                updateCmd.ExecuteNonQuery();
            }
        }

        public static void InitializeStaffRoster()
        {
            using var conn = new SqliteConnection(GetConnectionString());
            conn.Open();

            using var createCmd = conn.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS StaffRoster (
                    StaffId INTEGER PRIMARY KEY AUTOINCREMENT,
                    StaffNumber TEXT UNIQUE NOT NULL,
                    FullName TEXT NOT NULL,
                    Department TEXT NOT NULL,
                    AssignmentRole TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Phone TEXT NOT NULL,
                    Status TEXT DEFAULT 'Active',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            createCmd.ExecuteNonQuery();

            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM StaffRoster;";
            long count = (long)(countCmd.ExecuteScalar() ?? 0);

            if (count == 0)
            {
                var staffList = new[]
                {
                    ("STF-001", "Sarah Davis",        "Administration", "Principal",           "principal.davis@smartschool.edu",  "0917-100-2000"),
                    ("STF-002", "Director Vance",     "Discipline",     "Disciplinary Lead",   "vance.discipline@smartschool.edu", "0917-200-3000"),
                    ("STF-003", "Dr. Sarah Jenkins",  "Guidance",       "Case Investigator",   "jenkins.guidance@smartschool.edu", "0917-300-4000"),
                    ("STF-004", "John Smith",         "Academics",      "Homeroom Adviser",    "smith.teacher@smartschool.edu",    "0917-400-5000"),
                    ("STF-005", "Maria Santos",       "Office Staff",   "Records Assistant",   "staff.santos@smartschool.edu",     "0917-500-6000")
                };

                foreach (var (num, name, dept, role, email, phone) in staffList)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO StaffRoster (StaffNumber, FullName, Department, AssignmentRole, Email, Phone, Status, CreatedAt)
                        VALUES (@Num, @Name, @Dept, @Role, @Email, @Phone, 'Active', CURRENT_TIMESTAMP);";
                    cmd.Parameters.AddWithValue("@Num", num);
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Dept", dept);
                    cmd.Parameters.AddWithValue("@Role", role);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void SeedDefaultAccounts(SqliteConnection conn)
        {
            EnsureFourDemoAccounts(conn);
        }

        public static void EnsureFourDemoAccounts(SqliteConnection conn)
        {
            var accounts = new[]
            {
                ("Admin",              "Administrator",      "Admin",              "Admin123",      "Administrator"),
                ("staff",              "School Staff",       "staff",              "staff123",      "Staff"),
                ("discipline_lead",    "Discipline Officer", "discipline_lead",    "discipline123", "Discipline Officer"),
                ("guidance_counselor", "Guidance Counselor", "guidance_counselor", "guidance123", "Guidance Counselor"),
                ("admin",              "Administrator",      "admin@smartschool.edu", "Admin123",   "Administrator"),
                ("discipline",         "Discipline Officer", "discipline@smartschool.edu", "discipline123", "Discipline Officer"),
                ("guidance",           "Guidance Counselor", "guidance@smartschool.edu",   "guidance123",   "Guidance Counselor")
            };

            foreach (var (username, fullName, email, password, role) in accounts)
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM SystemUsers WHERE LOWER(Username) = LOWER(@Username) OR LOWER(Email) = LOWER(@Email);";
                checkCmd.Parameters.AddWithValue("@Username", username);
                checkCmd.Parameters.AddWithValue("@Email", email);
                long exists = (long)(checkCmd.ExecuteScalar() ?? 0);

                if (exists == 0)
                {
                    using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO SystemUsers (Username, FullName, Email, PasswordHash, Role, Status, CreatedAt)
                        VALUES (@Username, @FullName, @Email, @PasswordHash, @Role, 'Active', CURRENT_TIMESTAMP);";
                    insertCmd.Parameters.AddWithValue("@Username", username);
                    insertCmd.Parameters.AddWithValue("@FullName", fullName);
                    insertCmd.Parameters.AddWithValue("@Email", email);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                    insertCmd.Parameters.AddWithValue("@Role", role);
                    insertCmd.ExecuteNonQuery();
                }
                else
                {
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE SystemUsers
                        SET Username = @Username, PasswordHash = @PasswordHash, Role = @Role, FullName = @FullName, Email = @Email, Status = 'Active'
                        WHERE LOWER(Username) = LOWER(@Username) OR LOWER(Email) = LOWER(@Email);";
                    updateCmd.Parameters.AddWithValue("@Username", username);
                    updateCmd.Parameters.AddWithValue("@FullName", fullName);
                    updateCmd.Parameters.AddWithValue("@Email", email);
                    updateCmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                    updateCmd.Parameters.AddWithValue("@Role", role);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
