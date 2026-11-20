using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Helpers;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public static class AuditLogger
    {
        private static string _connectionString = string.Empty;

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTableCreated();
        }

        public static void EnsureTableCreated()
        {
            if (DatabaseProvider.IsMySql) return;

            try
            {
                using var conn = DatabaseProvider.CreateConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SystemAuditLogs (
                        LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UserId TEXT NOT NULL,
                        ActionType TEXT NOT NULL, 
                        ModuleName TEXT NOT NULL,
                        Description TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();

                // Seed initial audit trail if empty
                using var countCmd = conn.CreateCommand();
                countCmd.CommandText = "SELECT COUNT(*) FROM SystemAuditLogs;";
                long count = (long)(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO SystemAuditLogs (Timestamp, UserId, ActionType, ModuleName, Description) VALUES
                        (DATETIME('now', '-2 hours'), 'ADMIN-01', 'LOGIN', 'System Auth', 'User admin successfully logged into the system.'),
                        (DATETIME('now', '-90 minutes'), 'ADMIN-01', 'CREATE', 'Incident Intake', 'Created new incident record INC-2026-089 for student Juan Dela Cruz.'),
                        (DATETIME('now', '-60 minutes'), 'ADMIN-01', 'EXPORT', 'Parent Notices', 'Generated parent notice PDF for student STD-1042.'),
                        (DATETIME('now', '-30 minutes'), 'OFFICER-02', 'UPDATE', 'Action Plans', 'Updated Individual Action Plan milestone status for Michael Brown.'),
                        (DATETIME('now', '-10 minutes'), 'ADMIN-01', 'EXPORT', 'Audit Trail', 'Exported security audit trail ledger PDF.');";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public static async Task LogAsync(string userId, string actionType, string moduleName, string description)
        {
            EnsureTableCreated();

            try
            {
                using var conn = DatabaseProvider.CreateConnection();
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO SystemAuditLogs (Timestamp, UserId, ActionType, ModuleName, Description)
                    VALUES (CURRENT_TIMESTAMP, @UserId, @ActionType, @ModuleName, @Description);";

                cmd.AddParam("@UserId", string.IsNullOrWhiteSpace(userId) ? "SYSTEM" : userId);
                cmd.AddParam("@ActionType", actionType);
                cmd.AddParam("@ModuleName", moduleName);
                cmd.AddParam("@Description", description);

                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }
    }

    public interface IAuditService
    {
        Task<List<AuditLogModel>> GetAllAuditLogsAsync();
        Task LogActivityAsync(string userId, string actionType, string moduleName, string description);
    }

    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            AuditLogger.EnsureTableCreated();
        }

        public async Task<List<AuditLogModel>> GetAllAuditLogsAsync()
        {
            AuditLogger.EnsureTableCreated();
            var list = new List<AuditLogModel>();

            using var conn = DatabaseProvider.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT LogId, Timestamp, UserId, ActionType, ModuleName, Description FROM SystemAuditLogs ORDER BY LogId DESC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AuditLogModel
                {
                    LogId = reader.GetInt64(0),
                    Timestamp = reader.IsDBNull(1) ? DateTime.Now : reader.GetDateTime(1),
                    UserId = reader.GetString(2),
                    ActionType = reader.GetString(3),
                    ModuleName = reader.GetString(4),
                    Description = reader.GetString(5)
                });
            }

            return list;
        }

        public async Task LogActivityAsync(string userId, string actionType, string moduleName, string description)
        {
            await AuditLogger.LogAsync(userId, actionType, moduleName, description);
        }
    }
}