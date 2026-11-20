using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IAcknowledgmentService
    {
        Task EnsureDatabaseCreatedAsync();
        Task<List<AcknowledgmentLog>> GetAllLogsAsync();
        Task SaveLogAsync(AcknowledgmentLog log);
        Task UpdateAcknowledgmentAsync(string id, string status, string dateAcknowledged, string remarks, string attachedFilePath);
        Task DeleteLogAsync(string id);
    }

    public class AcknowledgmentService : IAcknowledgmentService
    {
        public AcknowledgmentService(AppDbContext? context = null)
        {
            EnsureTables();
        }

        private void EnsureTables()
        {
            if (DatabaseProvider.IsMySql) return;

            try
            {
                using var conn = DatabaseProvider.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AcknowledgmentLogs (
                        Id TEXT PRIMARY KEY,
                        NoticeId TEXT NOT NULL,
                        StudentName TEXT NOT NULL,
                        IssueDate TEXT NOT NULL,
                        DueDate TEXT NOT NULL,
                        Status TEXT DEFAULT 'Pending',
                        DateAcknowledged TEXT,
                        Remarks TEXT,
                        AttachedFilePath TEXT
                    );
                ";
                cmd.ExecuteNonQuery();

                using var countCmd = conn.CreateCommand();
                countCmd.CommandText = "SELECT COUNT(*) FROM AcknowledgmentLogs;";
                int count = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var seedCmd = conn.CreateCommand();
                    seedCmd.CommandText = @"
                        INSERT INTO AcknowledgmentLogs (Id, NoticeId, StudentName, IssueDate, DueDate, Status, DateAcknowledged, Remarks, AttachedFilePath)
                        VALUES
                        ('ack-001', 'NTC-2026-001', 'Juan Dela Cruz', '2026-08-20', '2026-08-25', 'Pending', '', '', ''),
                        ('ack-002', 'NTC-2026-002', 'Jane Smith', '2026-08-15', '2026-08-20', 'Acknowledged', '2026-08-18', 'Parent attended conference meeting in person. Discussed counseling roadmap.', 'Scanned_Slips/jane_smith_signed.pdf'),
                        ('ack-003', 'NTC-2026-003', 'Michael Brown', '2026-08-10', '2026-08-15', 'Overdue', '', '', ''),
                        ('ack-004', 'NTC-2026-004', 'Sarah Davis', '2026-08-22', '2026-08-28', 'Pending', '', '', '');
                    ";
                    seedCmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public Task EnsureDatabaseCreatedAsync()
        {
            EnsureTables();
            return Task.CompletedTask;
        }

        public async Task<List<AcknowledgmentLog>> GetAllLogsAsync()
        {
            EnsureTables();
            var list = new List<AcknowledgmentLog>();
            using var conn = DatabaseProvider.CreateConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, NoticeId, StudentName, IssueDate, DueDate, Status, DateAcknowledged, Remarks, AttachedFilePath FROM AcknowledgmentLogs ORDER BY IssueDate DESC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AcknowledgmentLog
                {
                    Id = reader.GetString(0),
                    NoticeId = reader.GetString(1),
                    StudentName = reader.GetString(2),
                    IssueDate = reader.GetString(3),
                    DueDate = reader.GetString(4),
                    Status = reader.IsDBNull(5) ? "Pending" : reader.GetString(5),
                    DateAcknowledged = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Remarks = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    AttachedFilePath = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                });
            }
            return list;
        }

        public async Task SaveLogAsync(AcknowledgmentLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            EnsureTables();

            using var conn = DatabaseProvider.CreateConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                {DatabaseProvider.InsertOrReplaceKeyword} INTO AcknowledgmentLogs (Id, NoticeId, StudentName, IssueDate, DueDate, Status, DateAcknowledged, Remarks, AttachedFilePath)
                VALUES (@Id, @NoticeId, @StudentName, @IssueDate, @DueDate, @Status, @DateAcknowledged, @Remarks, @AttachedFilePath);";

            AddParam(cmd, "@Id", log.Id);
            AddParam(cmd, "@NoticeId", log.NoticeId);
            AddParam(cmd, "@StudentName", log.StudentName);
            AddParam(cmd, "@IssueDate", log.IssueDate);
            AddParam(cmd, "@DueDate", log.DueDate);
            AddParam(cmd, "@Status", log.Status);
            AddParam(cmd, "@DateAcknowledged", log.DateAcknowledged);
            AddParam(cmd, "@Remarks", log.Remarks);
            AddParam(cmd, "@AttachedFilePath", log.AttachedFilePath);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAcknowledgmentAsync(string id, string status, string dateAcknowledged, string remarks, string attachedFilePath)
        {
            EnsureTables();

            using var conn = DatabaseProvider.CreateConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE AcknowledgmentLogs
                SET Status = @Status,
                    DateAcknowledged = @DateAcknowledged,
                    Remarks = @Remarks,
                    AttachedFilePath = @AttachedFilePath
                WHERE Id = @Id;";

            AddParam(cmd, "@Id", id);
            AddParam(cmd, "@Status", status);
            AddParam(cmd, "@DateAcknowledged", dateAcknowledged);
            AddParam(cmd, "@Remarks", remarks);
            AddParam(cmd, "@AttachedFilePath", attachedFilePath);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteLogAsync(string id)
        {
            EnsureTables();
            using var conn = DatabaseProvider.CreateConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AcknowledgmentLogs WHERE Id = @Id;";
            AddParam(cmd, "@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddParam(System.Data.Common.DbCommand cmd, string name, object val)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = val ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
