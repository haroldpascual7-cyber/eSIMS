using MySqlConnector;
using SmartSchoolIncidentManagementSystem.Data;
using System;
using System.Data.Common;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Initializes MySQL tables and seeds initial data for ancillary modules.
    /// Each statement is executed individually against MySQL InnoDB engine.
    /// </summary>
    public static class MySqlSchemaInitializer
    {
        public static void Initialize()
        {
            if (!DatabaseProvider.IsMySql) return;

            try
            {
                using var conn = DatabaseProvider.CreateConnection();
                conn.Open();

                ExecuteDdl(conn);
                SeedDataIfEmpty(conn);
            }
            catch (Exception)
            {
                // Allow startup to proceed even if ancillary schema had partial warnings
            }
        }

        private static void ExecuteDdl(DbConnection conn)
        {
            var statements = new[]
            {
                @"CREATE TABLE IF NOT EXISTS SystemAuditLogs (
                    LogId INT AUTO_INCREMENT PRIMARY KEY,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UserId VARCHAR(255) NOT NULL,
                    ActionType VARCHAR(100) NOT NULL,
                    ModuleName VARCHAR(255) NOT NULL,
                    Description LONGTEXT NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS AccessSecurityLogs (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    AttemptedUsername VARCHAR(255) NOT NULL,
                    MachineName VARCHAR(255) NOT NULL,
                    EventType VARCHAR(100) NOT NULL,
                    Status VARCHAR(100) NOT NULL,
                    Details LONGTEXT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS IndividualActionPlans (
                    Id VARCHAR(255) PRIMARY KEY,
                    StudentId VARCHAR(255) NOT NULL,
                    StudentName VARCHAR(255) NOT NULL,
                    GradeSection VARCHAR(255) DEFAULT 'Grade 10 - St. Thomas',
                    FocusArea VARCHAR(255) NOT NULL,
                    AssignedCounselor VARCHAR(255) NOT NULL,
                    RiskTier VARCHAR(100) NOT NULL,
                    StartDate VARCHAR(50) NOT NULL,
                    ReviewDate VARCHAR(50) NOT NULL,
                    ProblemStatement LONGTEXT,
                    CoreRoadmapObjectives LONGTEXT,
                    Status VARCHAR(50) DEFAULT 'Active',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS IapMilestones (
                    MilestoneId VARCHAR(255) PRIMARY KEY,
                    PlanId VARCHAR(255) NOT NULL,
                    Title VARCHAR(255) NOT NULL,
                    DueDate VARCHAR(50),
                    IsCompleted INT DEFAULT 0
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS IapCounselorNotes (
                    NoteId VARCHAR(255) PRIMARY KEY,
                    PlanId VARCHAR(255) NOT NULL,
                    Author VARCHAR(255) NOT NULL,
                    NoteDate VARCHAR(50) NOT NULL,
                    Content LONGTEXT NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS IapAttachments (
                    AttachmentId VARCHAR(255) PRIMARY KEY,
                    PlanId VARCHAR(255) NOT NULL,
                    FileName VARCHAR(255) NOT NULL,
                    FilePath VARCHAR(500) NOT NULL,
                    UploadDate VARCHAR(50) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS AcknowledgmentLogs (
                    Id VARCHAR(255) PRIMARY KEY,
                    NoticeId VARCHAR(255) NOT NULL,
                    StudentName VARCHAR(255) NOT NULL,
                    IssueDate VARCHAR(50) NOT NULL,
                    DueDate VARCHAR(50) NOT NULL,
                    Status VARCHAR(50) DEFAULT 'Pending',
                    DateAcknowledged VARCHAR(50),
                    Remarks LONGTEXT,
                    AttachedFilePath VARCHAR(500)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS DispatchLogs (
                    Id VARCHAR(255) PRIMARY KEY,
                    StudentId VARCHAR(255) NOT NULL,
                    GuardianName VARCHAR(255) NOT NULL,
                    ContactDestination VARCHAR(255) NOT NULL,
                    Channel VARCHAR(50) NOT NULL,
                    MessageBody LONGTEXT NOT NULL,
                    Status VARCHAR(50) NOT NULL,
                    SentAt DATETIME DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

                @"CREATE TABLE IF NOT EXISTS LetterTemplates (
                    Id VARCHAR(255) PRIMARY KEY,
                    TemplateName VARCHAR(255) NOT NULL,
                    TemplateBody LONGTEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
            };

            foreach (var sql in statements)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedDataIfEmpty(DbConnection conn)
        {
            // 1. Audit Logs
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM SystemAuditLogs;";
                long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var ins = conn.CreateCommand();
                    ins.CommandText = @"
                        INSERT INTO SystemAuditLogs (Timestamp, UserId, ActionType, ModuleName, Description) VALUES
                        (DATE_SUB(NOW(), INTERVAL 2 HOUR), 'ADMIN-01', 'LOGIN', 'System Auth', 'User admin successfully logged into the system.'),
                        (DATE_SUB(NOW(), INTERVAL 90 MINUTE), 'ADMIN-01', 'CREATE', 'Incident Intake', 'Created new incident record INC-2026-089 for student Juan Dela Cruz.'),
                        (DATE_SUB(NOW(), INTERVAL 60 MINUTE), 'ADMIN-01', 'EXPORT', 'Parent Notices', 'Generated parent notice PDF for student STD-1042.'),
                        (DATE_SUB(NOW(), INTERVAL 30 MINUTE), 'OFFICER-02', 'UPDATE', 'Action Plans', 'Updated Individual Action Plan milestone status for Michael Brown.'),
                        (DATE_SUB(NOW(), INTERVAL 10 MINUTE), 'ADMIN-01', 'EXPORT', 'Audit Trail', 'Exported security audit trail ledger PDF.');";
                    ins.ExecuteNonQuery();
                }
            }

            // 2. Access Security Logs
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM AccessSecurityLogs;";
                long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    string machine = Environment.MachineName.Replace("'", "''");
                    using var ins = conn.CreateCommand();
                    ins.CommandText = $@"
                        INSERT INTO AccessSecurityLogs (Timestamp, AttemptedUsername, MachineName, EventType, Status, Details) VALUES
                        (DATE_SUB(NOW(), INTERVAL 3 HOUR), 'admin', '{machine}', 'Login', 'Success', 'User authenticated successfully from local workstation.'),
                        (DATE_SUB(NOW(), INTERVAL 2 HOUR), 'johndoe', '{machine}', 'Failed Login', 'Failed', 'Invalid password entered (Attempt 1 of 3).'),
                        (DATE_SUB(NOW(), INTERVAL 110 MINUTE), 'johndoe', '{machine}', 'Failed Login', 'Failed', 'Invalid password entered (Attempt 2 of 3).'),
                        (DATE_SUB(NOW(), INTERVAL 100 MINUTE), 'johndoe', '{machine}', 'Account Lockout', 'Blocked', 'Account locked after 3 consecutive failed login attempts.'),
                        (DATE_SUB(NOW(), INTERVAL 45 MINUTE), 'officer1', '{machine}', 'Login', 'Success', 'Discipline officer session established.'),
                        (DATE_SUB(NOW(), INTERVAL 20 MINUTE), 'admin', '{machine}', 'Role Change', 'Success', 'Updated role permissions for user officer1.');";
                    ins.ExecuteNonQuery();
                }
            }

            // 3. Individual Action Plans
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM IndividualActionPlans;";
                long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var ins1 = conn.CreateCommand();
                    ins1.CommandText = @"
                        INSERT INTO IndividualActionPlans (Id, StudentId, StudentName, GradeSection, FocusArea, AssignedCounselor, RiskTier, StartDate, ReviewDate, ProblemStatement, CoreRoadmapObjectives, Status)
                        VALUES ('IAP-2026-001', 'STD-1042', 'Juan Dela Cruz', 'Grade 10 - St. Thomas', 'Anger Management & De-escalation', 'David Taylor', 'Tier 3 - Critical', '2026-08-01', '2026-09-01', 'Repeated physical altercations during playground recreation.', 'Achieve emotional self-regulation and complete restorative apology agreement.', 'Active');";
                    ins1.ExecuteNonQuery();

                    using var ins2 = conn.CreateCommand();
                    ins2.CommandText = @"
                        INSERT INTO IapMilestones (MilestoneId, PlanId, Title, DueDate, IsCompleted) VALUES
                        ('m1', 'IAP-2026-001', 'Complete initial intake assessment & trigger mapping session', '2026-08-05', 1),
                        ('m2', 'IAP-2026-001', 'Attend bi-weekly anger de-escalation counseling Module 1', '2026-08-15', 1),
                        ('m3', 'IAP-2026-001', 'Submit signed peer restorative circle agreement', '2026-08-25', 1),
                        ('m4', 'IAP-2026-001', 'Maintain zero playground infraction strikes for 30 consecutive days', '2026-09-01', 0);";
                    ins2.ExecuteNonQuery();

                    using var ins3 = conn.CreateCommand();
                    ins3.CommandText = @"
                        INSERT INTO IapCounselorNotes (NoteId, PlanId, Author, NoteDate, Content) VALUES
                        ('n1', 'IAP-2026-001', 'David Taylor (Counselor)', '2026-08-05 10:30', 'Student attended intake session receptive to guidance.'),
                        ('n2', 'IAP-2026-001', 'David Taylor (Counselor)', '2026-08-18 14:15', 'Completed counseling Module 1.');";
                    ins3.ExecuteNonQuery();
                }
            }

            // 4. Acknowledgment Logs
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM AcknowledgmentLogs;";
                long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var ins = conn.CreateCommand();
                    ins.CommandText = @"
                        INSERT INTO AcknowledgmentLogs (Id, NoticeId, StudentName, IssueDate, DueDate, Status, DateAcknowledged, Remarks, AttachedFilePath) VALUES
                        ('ack-001', 'NTC-2026-001', 'Juan Dela Cruz', '2026-08-20', '2026-08-25', 'Pending', '', '', ''),
                        ('ack-002', 'NTC-2026-002', 'Jane Smith', '2026-08-15', '2026-08-20', 'Acknowledged', '2026-08-18', 'Parent attended conference meeting in person.', 'Scanned_Slips/jane_smith_signed.pdf'),
                        ('ack-003', 'NTC-2026-003', 'Michael Brown', '2026-08-10', '2026-08-15', 'Overdue', '', '', ''),
                        ('ack-004', 'NTC-2026-004', 'Sarah Davis', '2026-08-22', '2026-08-28', 'Pending', '', '', '');";
                    ins.ExecuteNonQuery();
                }
            }

            // 5. Dispatch Logs
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM DispatchLogs;";
                long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var ins = conn.CreateCommand();
                    ins.CommandText = @"
                        INSERT INTO DispatchLogs (Id, StudentId, GuardianName, ContactDestination, Channel, MessageBody, Status, SentAt) VALUES
                        ('dsp-001', 'STD-1042', 'Maria Dela Cruz', '+63 917-555-1234', 'SMS', 'Smart Academy Alert: Behavioral infraction notification.', 'Sent', DATE_SUB(NOW(), INTERVAL 2 HOUR)),
                        ('dsp-002', 'STD-1002', 'Sarah Smith', 'sarah.smith@gmail.com', 'Email', 'Smart Academy Notification: Parent-Counselor Conference.', 'Sent', DATE_SUB(NOW(), INTERVAL 1 DAY)),
                        ('dsp-003', 'STD-1005', 'Patricia Brown', '+63 917-999-0000', 'SMS', 'Urgent Notice: Suspension order issued.', 'Sent', DATE_SUB(NOW(), INTERVAL 3 DAY));";
                    ins.ExecuteNonQuery();
                }
            }

            // 6. Letter Templates
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM LetterTemplates;";
                long count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var ins = conn.CreateCommand();
                    ins.CommandText = @"
                        INSERT INTO LetterTemplates (Id, TemplateName, TemplateBody, CreatedAt) VALUES
                        ('tpl-001', 'Parent Conference Summons', 'OFFICIAL NOTICE OF PARENT / GUARDIAN CONFERENCE\n\nDate: {{ActionDate}}\nTo the Parent/Guardian of {{StudentName}} (ID: {{StudentId}}):\n\nThis is an official communication regarding an incident on {{IncidentDate}} at {{Location}}.', NOW()),
                        ('tpl-002', 'Notice of Suspension Order', 'OFFICIAL DISCIPLINARY NOTICE: SUSPENSION ORDER\n\nDate: {{ActionDate}}\nStudent: {{StudentName}} (ID: {{StudentId}})\nGrade/Section: {{GradeSection}}\n\nEffective {{SuspensionStartDate}} to {{SuspensionEndDate}}.', NOW()),
                        ('tpl-003', 'Notice of Disciplinary Inquiry', 'OFFICIAL NOTICE OF DISCIPLINARY INQUIRY\n\nDate: {{ActionDate}}\nTo: Parent/Guardian of {{StudentName}}\n\nPlease be informed of an inquiry regarding {{Infraction}}.', NOW());";
                    ins.ExecuteNonQuery();
                }
            }
        }
    }
}
