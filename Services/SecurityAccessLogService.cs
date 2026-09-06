using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface ISecurityAccessLogService
    {
        Task<List<AccessSecurityLogModel>> GetAccessLogsAsync();
        Task LogAccessEventAsync(string username, string eventType, string status, string details);
        Task<bool> UnlockAccountAsync(string username);
    }

    public class SecurityAccessLogService : ISecurityAccessLogService
    {
        private readonly AppDbContext _context;
        private string _connectionString = string.Empty;

        public SecurityAccessLogService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            EnsureTableCreated();
        }

        public void EnsureTableCreated()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "incidents.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                _connectionString = $"Data Source={dbPath}";
            }

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AccessSecurityLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        AttemptedUsername TEXT NOT NULL,
                        MachineName TEXT NOT NULL,
                        EventType TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Details TEXT
                    );";
                cmd.ExecuteNonQuery();

                // Seed initial data if empty
                using var countCmd = conn.CreateCommand();
                countCmd.CommandText = "SELECT COUNT(*) FROM AccessSecurityLogs;";
                long count = (long)(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    string machine = Environment.MachineName;
                    cmd.CommandText = $@"
                        INSERT INTO AccessSecurityLogs (Timestamp, AttemptedUsername, MachineName, EventType, Status, Details) VALUES
                        (DATETIME('now', '-3 hours'), 'admin', '{machine}', 'Login', 'Success', 'User authenticated successfully from local workstation.'),
                        (DATETIME('now', '-2 hours'), 'johndoe', '{machine}', 'Failed Login', 'Failed', 'Invalid password entered (Attempt 1 of 3).'),
                        (DATETIME('now', '-110 minutes'), 'johndoe', '{machine}', 'Failed Login', 'Failed', 'Invalid password entered (Attempt 2 of 3).'),
                        (DATETIME('now', '-100 minutes'), 'johndoe', '{machine}', 'Account Lockout', 'Blocked', 'Account locked after 3 consecutive failed login attempts.'),
                        (DATETIME('now', '-45 minutes'), 'officer1', '{machine}', 'Login', 'Success', 'Discipline officer session established.'),
                        (DATETIME('now', '-20 minutes'), 'admin', '{machine}', 'Role Change', 'Success', 'Updated role permissions for user officer1.');";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public async Task<List<AccessSecurityLogModel>> GetAccessLogsAsync()
        {
            EnsureTableCreated();
            var list = new List<AccessSecurityLogModel>();

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Timestamp, AttemptedUsername, MachineName, EventType, Status, Details FROM AccessSecurityLogs ORDER BY Id DESC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AccessSecurityLogModel
                {
                    Id = reader.GetInt64(0),
                    Timestamp = reader.IsDBNull(1) ? DateTime.Now : reader.GetDateTime(1),
                    AttemptedUsername = reader.GetString(2),
                    MachineName = reader.GetString(3),
                    EventType = reader.GetString(4),
                    Status = reader.GetString(5),
                    Details = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                });
            }

            return list;
        }

        public async Task LogAccessEventAsync(string username, string eventType, string status, string details)
        {
            EnsureTableCreated();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO AccessSecurityLogs (Timestamp, AttemptedUsername, MachineName, EventType, Status, Details)
                    VALUES (CURRENT_TIMESTAMP, @Username, @MachineName, @EventType, @Status, @Details);";

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName);
                cmd.Parameters.AddWithValue("@EventType", eventType);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Details", details);

                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }

        public async Task<bool> UnlockAccountAsync(string username)
        {
            EnsureTableCreated();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Users 
                    SET IsLocked = 0, FailedAttempts = 0 
                    WHERE Username = @Username;";
                cmd.Parameters.AddWithValue("@Username", username);

                await cmd.ExecuteNonQueryAsync();

                await LogAccessEventAsync(username, "Account Unlocked", "Success", $"Account unlocked manually by administrator from security control panel.");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}