using MySqlConnector;
using SmartSchoolIncidentManagementSystem.Data;
using System;
using System.Collections.Generic;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Helper to synchronize and upgrade MySQL schema columns to ensure existing databases
    /// match the entity models seamlessly without runtime query failures.
    /// </summary>
    public static class MySqlSchemaUpgrader
    {
        public static void EnsureSchemaUpToDate()
        {
            if (!DatabaseProvider.IsMySql) return;

            try
            {
                using var conn = new MySqlConnection(DatabaseProvider.GetConnectionString());
                conn.Open();

                // 1. Incidents table columns
                EnsureColumnExists(conn, "Incidents", "DescriptionEncrypted", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "Description", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "CreatedBy", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Incidents", "InvolvedPersons", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "Witnesses", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "EvidenceData", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "ActionTaken", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "Notes", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "RecommendedAction", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Incidents", "IsRepeatIncident", "TINYINT(1) NOT NULL DEFAULT 0");
                EnsureColumnExists(conn, "Incidents", "RepeatIncidentCount", "INT NOT NULL DEFAULT 0");

                // 2. Students table columns
                EnsureColumnExists(conn, "Students", "MiddleName", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "Gender", "VARCHAR(50) NOT NULL DEFAULT 'Male'");
                EnsureColumnExists(conn, "Students", "DateOfBirth", "DATETIME NULL");
                EnsureColumnExists(conn, "Students", "Email", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "Phone", "VARCHAR(100) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "Address", "VARCHAR(500) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "TrackStrand", "VARCHAR(255) NOT NULL DEFAULT 'Academic'");
                EnsureColumnExists(conn, "Students", "Adviser", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "GuardianName", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "GuardianRelation", "VARCHAR(100) NOT NULL DEFAULT 'Parent'");
                EnsureColumnExists(conn, "Students", "GuardianContact", "VARCHAR(100) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "GuardianEmail", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Students", "MedicalNotes", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Students", "ProfilePicturePath", "VARCHAR(1000) NOT NULL DEFAULT ''");

                // 3. Investigations table columns
                EnsureColumnExists(conn, "Investigations", "LeadInvestigatorName", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Investigations", "InvestigationTimeline", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Investigations", "IsFollowUpFlagged", "TINYINT(1) NOT NULL DEFAULT 0");
                EnsureColumnExists(conn, "Investigations", "FollowUpIssueType", "VARCHAR(255) NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, "Investigations", "FollowUpNotes", "LONGTEXT NULL");
                EnsureColumnExists(conn, "Investigations", "IsFollowUpResolved", "TINYINT(1) NOT NULL DEFAULT 0");

                // 4. SuspensionOrders table columns
                EnsureColumnExists(conn, "SuspensionOrders", "IncidentId", "INT NULL");
                EnsureColumnExists(conn, "SuspensionOrders", "StudentId", "INT NULL");
                EnsureColumnExists(conn, "SuspensionOrders", "Reason", "LONGTEXT NULL");

                // 5. SystemUsers table column
                EnsureColumnExists(conn, "SystemUsers", "ProfilePicturePath", "VARCHAR(1000) NULL");
            }
            catch
            {
                // Ignore errors during upgrade check to allow application flow to continue
            }
        }

        private static void EnsureColumnExists(MySqlConnection conn, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                      AND TABLE_NAME = @tableName 
                      AND COLUMN_NAME = @columnName;";
                checkCmd.Parameters.AddWithValue("@tableName", tableName);
                checkCmd.Parameters.AddWithValue("@columnName", columnName);

                long exists = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);
                if (exists == 0)
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {columnDefinition};";
                    alterCmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Fallback attempt or column already present
            }
        }
    }
}
