using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface ICommunicationService
    {
        Task EnsureDatabaseCreatedAsync();
        Task<bool> SendSmsAsync(string studentId, string guardianName, string phoneNumber, string message);
        Task<bool> SendEmailAsync(string studentId, string guardianName, string emailAddress, string subject, string message);
        Task<bool> SendExecutiveSummaryEmailAsync(string pdfPath, string recipientEmail);
        Task<List<DispatchLog>> GetRecentDispatchesAsync(int count = 50);
    }

    public class CommunicationService : ICommunicationService
    {
        private readonly AppDbContext _context;

        public CommunicationService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            EnsureTables();
        }

        private void EnsureTables()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS DispatchLogs (
                        Id TEXT PRIMARY KEY,
                        StudentId TEXT NOT NULL,
                        GuardianName TEXT NOT NULL,
                        ContactDestination TEXT NOT NULL,
                        Channel TEXT NOT NULL,
                        MessageBody TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        SentAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ");

                int count = _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM DispatchLogs;").AsEnumerable().FirstOrDefault();
                if (count == 0)
                {
                    _context.Database.ExecuteSqlRaw(@"
                        INSERT INTO DispatchLogs (Id, StudentId, GuardianName, ContactDestination, Channel, MessageBody, Status, SentAt)
                        VALUES
                        ('dsp-001', 'STD-1042', 'Maria Dela Cruz', '+63 917-555-1234', 'SMS', 'Smart Academy Alert: Please be advised that Juan Dela Cruz was involved in a behavioral infraction today. Check email for details.', 'Sent', datetime('now', '-2 hours')),
                        ('dsp-002', 'STD-1002', 'Sarah Smith', 'sarah.smith@gmail.com', 'Email', 'Smart Academy Notification: Formal Notice of Parent-Counselor Conference scheduled for Jane Smith on 2026-08-29.', 'Sent', datetime('now', '-1 day')),
                        ('dsp-003', 'STD-1005', 'Patricia Brown', '+63 917-999-0000', 'SMS', 'Urgent Notice: Michael Brown has been issued a Suspension Order starting tomorrow. Please contact Guidance Office.', 'Sent', datetime('now', '-3 days'));
                    ");
                }
            }
            catch { }
        }

        public Task EnsureDatabaseCreatedAsync()
        {
            EnsureTables();
            return Task.CompletedTask;
        }

        public async Task<bool> SendSmsAsync(string studentId, string guardianName, string phoneNumber, string message)
        {
            EnsureTables();
            
            // SIMULATION MODE: Simulating API Gateway network delay (e.g. Twilio / Semaphore API)
            // TO INJECT REAL TWILIO:
            // TwilioClient.Init("ACCOUNT_SID", "AUTH_TOKEN");
            // var sms = MessageResource.Create(body: message, from: new PhoneNumber("+123456789"), to: new PhoneNumber(phoneNumber));

            await Task.Delay(1000); // Simulated dispatch latency

            string logId = Guid.NewGuid().ToString();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DispatchLogs (Id, StudentId, GuardianName, ContactDestination, Channel, MessageBody, Status, SentAt)
                VALUES (@Id, @StudentId, @GuardianName, @ContactDestination, 'SMS', @MessageBody, 'Sent', @SentAt);";

            AddParam(cmd, "@Id", logId);
            AddParam(cmd, "@StudentId", studentId);
            AddParam(cmd, "@GuardianName", guardianName);
            AddParam(cmd, "@ContactDestination", string.IsNullOrWhiteSpace(phoneNumber) ? "N/A" : phoneNumber);
            AddParam(cmd, "@MessageBody", message);
            AddParam(cmd, "@SentAt", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> SendEmailAsync(string studentId, string guardianName, string emailAddress, string subject, string message)
        {
            EnsureTables();

            // SIMULATION MODE: Simulating SMTP dispatch (e.g. MailKit / SmtpClient)
            // TO INJECT REAL SMTP:
            // using var client = new SmtpClient("smtp.smartschool.edu", 587) { Credentials = new NetworkCredential("alerts@smartschool.edu", "pass") };
            // client.Send(new MailMessage("alerts@smartschool.edu", emailAddress, subject, message));

            await Task.Delay(1000); // Simulated email latency

            string logId = Guid.NewGuid().ToString();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DispatchLogs (Id, StudentId, GuardianName, ContactDestination, Channel, MessageBody, Status, SentAt)
                VALUES (@Id, @StudentId, @GuardianName, @ContactDestination, 'Email', @MessageBody, 'Sent', @SentAt);";

            AddParam(cmd, "@Id", logId);
            AddParam(cmd, "@StudentId", studentId);
            AddParam(cmd, "@GuardianName", guardianName);
            AddParam(cmd, "@ContactDestination", string.IsNullOrWhiteSpace(emailAddress) ? "N/A" : emailAddress);
            AddParam(cmd, "@MessageBody", $"[Subject: {subject}]\n{message}");
            AddParam(cmd, "@SentAt", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<List<DispatchLog>> GetRecentDispatchesAsync(int count = 50)
        {
            EnsureTables();
            var list = new List<DispatchLog>();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, StudentId, GuardianName, ContactDestination, Channel, MessageBody, Status, SentAt FROM DispatchLogs ORDER BY SentAt DESC LIMIT {count};";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DispatchLog
                {
                    Id = reader.GetString(0),
                    StudentId = reader.GetString(1),
                    GuardianName = reader.GetString(2),
                    ContactDestination = reader.GetString(3),
                    Channel = reader.GetString(4),
                    MessageBody = reader.GetString(5),
                    Status = reader.GetString(6),
                    SentAt = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7)
                });
            }

            return list;
        }

        public async Task<bool> SendExecutiveSummaryEmailAsync(string pdfPath, string recipientEmail)
        {
            EnsureTables();
            await Task.Delay(1000);

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DispatchLogs (Id, StudentId, GuardianName, ContactDestination, Channel, MessageBody, Status, SentAt)
                VALUES (@Id, @StudentId, @GuardianName, @ContactDestination, 'Email', @MessageBody, 'Delivered', @SentAt);";

            AddParam(cmd, "@Id", Guid.NewGuid().ToString());
            AddParam(cmd, "@StudentId", "EXEC-BOARD");
            AddParam(cmd, "@GuardianName", "School Board / Principal");
            AddParam(cmd, "@ContactDestination", recipientEmail);
            AddParam(cmd, "@MessageBody", $"Executive Safety & Incident Report attached: {System.IO.Path.GetFileName(pdfPath)}");
            AddParam(cmd, "@SentAt", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
            return true;
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