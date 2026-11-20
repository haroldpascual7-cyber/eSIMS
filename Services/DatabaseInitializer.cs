using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Helpers;
using System;
using System.Data.Common;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Static initializer for system database tables (SystemUsers, RolePermissions, StaffRoster).
    /// Creates schemas and seeds initial records if tables are empty.
    /// Supports both SQLite and MySQL backends.
    /// </summary>
    public static class DatabaseInitializer
    {
        public static string GetConnectionString()
        {
            return DatabaseProvider.GetConnectionString();
        }

        public static string HashPassword(string password)
        {
            return PasswordHasher.Hash(password);
        }

        public static void InitializeAllTables()
        {
            InitializeSystemUsers();
            InitializeRolePermissions();
            InitializeStaffRoster();
            if (DatabaseProvider.IsMySql)
            {
                MySqlSchemaInitializer.Initialize();
            }
        }

        public static void InitializeSystemUsers()
        {
            using var conn = DatabaseProvider.CreateConnection();
            conn.Open();

            using var createCmd = conn.CreateCommand();
            if (DatabaseProvider.IsMySql)
            {
                createCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SystemUsers (
                        UserId INT AUTO_INCREMENT PRIMARY KEY,
                        Username VARCHAR(100) UNIQUE NOT NULL,
                        FullName VARCHAR(200) NOT NULL,
                        Email VARCHAR(255) NOT NULL,
                        PasswordHash VARCHAR(500) NOT NULL,
                        Role VARCHAR(100) NOT NULL,
                        Status VARCHAR(50) DEFAULT 'Active',
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ProfilePicturePath VARCHAR(1000) NULL
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            }
            else
            {
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
            }
            createCmd.ExecuteNonQuery();

            if (!DatabaseProvider.IsMySql)
            {
                try
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE SystemUsers ADD COLUMN ProfilePicturePath TEXT;";
                    alterCmd.ExecuteNonQuery();
                }
                catch { /* Column already exists */ }
            }

            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM SystemUsers;";
            long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);

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
            using var conn = DatabaseProvider.CreateConnection();
            conn.Open();

            using var createCmd = conn.CreateCommand();
            if (DatabaseProvider.IsMySql)
            {
                createCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RolePermissions (
                        PermissionId INT AUTO_INCREMENT PRIMARY KEY,
                        ModuleName VARCHAR(255) UNIQUE NOT NULL,
                        AdminAccess INT DEFAULT 1,
                        DisciplineOfficerAccess INT DEFAULT 1,
                        GuidanceCounselorAccess INT DEFAULT 1,
                        TeacherAccess INT DEFAULT 1,
                        StaffAccess INT DEFAULT 1,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            }
            else
            {
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
            }
            createCmd.ExecuteNonQuery();

            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM RolePermissions;";
            long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);

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
                    cmd.AddParam("@Mod", mod);
                    cmd.AddParam("@Adm", adm);
                    cmd.AddParam("@Disc", disc);
                    cmd.AddParam("@Guid", guid);
                    cmd.AddParam("@Teach", teach);
                    cmd.AddParam("@Stf", stf);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM RolePermissions WHERE ModuleName LIKE '%Investigation%';";
                long invCount = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);
                if (invCount == 0)
                {
                    using var insCmd = conn.CreateCommand();
                    insCmd.CommandText = @"
                        INSERT INTO RolePermissions (ModuleName, AdminAccess, DisciplineOfficerAccess, GuidanceCounselorAccess, TeacherAccess, StaffAccess, UpdatedAt)
                        VALUES ('Investigations & Evidence', 1, 1, 1, 1, 1, CURRENT_TIMESTAMP);";
                    insCmd.ExecuteNonQuery();
                }

                using var updateCmd1 = conn.CreateCommand();
                updateCmd1.CommandText = @"
                    UPDATE RolePermissions 
                    SET TeacherAccess = 1, StaffAccess = 1 
                    WHERE ModuleName IN ('Parent Notices & Letters', 'Student Profiles & IAPs', 'Investigations & Evidence', 'Reports & Safety Analytics', 'Incident Intake & Reporting')
                      AND (TeacherAccess = 0 OR StaffAccess = 0);";
                updateCmd1.ExecuteNonQuery();

                using var updateCmd2 = conn.CreateCommand();
                updateCmd2.CommandText = @"
                    UPDATE RolePermissions
                    SET GuidanceCounselorAccess = 1
                    WHERE ModuleName = 'Disciplinary Actions & Sanctions';";
                updateCmd2.ExecuteNonQuery();
            }
        }

        public static void InitializeStaffRoster()
        {
            using var conn = DatabaseProvider.CreateConnection();
            conn.Open();

            using var createCmd = conn.CreateCommand();
            if (DatabaseProvider.IsMySql)
            {
                createCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS StaffRoster (
                        StaffId INT AUTO_INCREMENT PRIMARY KEY,
                        StaffNumber VARCHAR(100) UNIQUE NOT NULL,
                        FullName VARCHAR(200) NOT NULL,
                        Department VARCHAR(100) NOT NULL,
                        AssignmentRole VARCHAR(100) NOT NULL,
                        Email VARCHAR(255) NOT NULL,
                        Phone VARCHAR(50) NOT NULL,
                        Status VARCHAR(50) DEFAULT 'Active',
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            }
            else
            {
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
            }
            createCmd.ExecuteNonQuery();

            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM StaffRoster;";
            long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);

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
                    cmd.AddParam("@Num", num);
                    cmd.AddParam("@Name", name);
                    cmd.AddParam("@Dept", dept);
                    cmd.AddParam("@Role", role);
                    cmd.AddParam("@Email", email);
                    cmd.AddParam("@Phone", phone);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void SeedDefaultAccounts(DbConnection conn)
        {
            EnsureFourDemoAccounts(conn);
        }

        public static void EnsureFourDemoAccounts(DbConnection conn)
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
                checkCmd.AddParam("@Username", username);
                checkCmd.AddParam("@Email", email);
                long exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);

                if (exists == 0)
                {
                    using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO SystemUsers (Username, FullName, Email, PasswordHash, Role, Status, CreatedAt)
                        VALUES (@Username, @FullName, @Email, @PasswordHash, @Role, 'Active', CURRENT_TIMESTAMP);";
                    insertCmd.AddParam("@Username", username);
                    insertCmd.AddParam("@FullName", fullName);
                    insertCmd.AddParam("@Email", email);
                    insertCmd.AddParam("@PasswordHash", HashPassword(password));
                    insertCmd.AddParam("@Role", role);
                    insertCmd.ExecuteNonQuery();
                }
                else
                {
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE SystemUsers
                        SET Username = @Username, Role = @Role, FullName = @FullName, Email = @Email, Status = 'Active'
                        WHERE LOWER(Username) = LOWER(@Username) OR LOWER(Email) = LOWER(@Email);";
                    updateCmd.AddParam("@Username", username);
                    updateCmd.AddParam("@FullName", fullName);
                    updateCmd.AddParam("@Email", email);
                    updateCmd.AddParam("@Role", role);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
