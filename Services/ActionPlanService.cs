using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IActionPlanService
    {
        Task EnsureDatabaseCreatedAsync();
        Task<List<IndividualActionPlan>> GetAllPlansAsync();
        Task<IndividualActionPlan?> GetPlanByIdAsync(string planId);
        Task InsertPlanAsync(IndividualActionPlan plan);
        Task UpdatePlanAsync(IndividualActionPlan plan);
        Task AddMilestoneAsync(IapMilestone milestone);
        Task ToggleMilestoneAsync(string milestoneId, bool isCompleted);
        Task AddNoteAsync(IapCounselorNote note);
        Task AddAttachmentAsync(IapAttachment attachment);
    }

    public class ActionPlanService : IActionPlanService
    {
        private readonly AppDbContext _context;

        public ActionPlanService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            EnsureTables();
        }

        private void EnsureTables()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS IndividualActionPlans (
                        Id TEXT PRIMARY KEY,
                        StudentId TEXT NOT NULL,
                        StudentName TEXT NOT NULL,
                        GradeSection TEXT DEFAULT 'Grade 10 - St. Thomas',
                        FocusArea TEXT NOT NULL,
                        AssignedCounselor TEXT NOT NULL,
                        RiskTier TEXT NOT NULL,
                        StartDate TEXT NOT NULL,
                        ReviewDate TEXT NOT NULL,
                        ProblemStatement TEXT,
                        CoreRoadmapObjectives TEXT,
                        Status TEXT DEFAULT 'Active',
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS IapMilestones (
                        MilestoneId TEXT PRIMARY KEY,
                        PlanId TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        DueDate TEXT,
                        IsCompleted INTEGER DEFAULT 0
                    );
                    CREATE TABLE IF NOT EXISTS IapCounselorNotes (
                        NoteId TEXT PRIMARY KEY,
                        PlanId TEXT NOT NULL,
                        Author TEXT NOT NULL,
                        NoteDate TEXT NOT NULL,
                        Content TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS IapAttachments (
                        AttachmentId TEXT PRIMARY KEY,
                        PlanId TEXT NOT NULL,
                        FileName TEXT NOT NULL,
                        FilePath TEXT NOT NULL,
                        UploadDate TEXT NOT NULL
                    );
                ");

                // Seed sample plan for Juan Dela Cruz if empty
                int planCount = _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM IndividualActionPlans").AsEnumerable().FirstOrDefault();
                if (planCount == 0)
                {
                    _context.Database.ExecuteSqlRaw(@"
                        INSERT INTO IndividualActionPlans (Id, StudentId, StudentName, GradeSection, FocusArea, AssignedCounselor, RiskTier, StartDate, ReviewDate, ProblemStatement, CoreRoadmapObjectives, Status)
                        VALUES (
                            'IAP-2026-001',
                            'STD-1042',
                            'Juan Dela Cruz',
                            'Grade 10 - St. Thomas',
                            'Anger Management & De-escalation',
                            'David Taylor',
                            'Tier 3 - Critical',
                            '2026-08-01',
                            '2026-09-01',
                            'Repeated physical altercations during playground recreation and elevated hostility during peer disputes.',
                            'Achieve emotional self-regulation, participate in 4 anger management counseling sessions, and complete restorative apology agreement.',
                            'Active'
                        );
                        INSERT INTO IapMilestones (MilestoneId, PlanId, Title, DueDate, IsCompleted)
                        VALUES 
                        ('m1', 'IAP-2026-001', 'Complete initial intake assessment & trigger mapping session', '2026-08-05', 1),
                        ('m2', 'IAP-2026-001', 'Attend bi-weekly anger de-escalation counseling Module 1', '2026-08-15', 1),
                        ('m3', 'IAP-2026-001', 'Submit signed peer restorative circle agreement', '2026-08-25', 1),
                        ('m4', 'IAP-2026-001', 'Maintain zero playground infraction strikes for 30 consecutive days', '2026-09-01', 0);

                        INSERT INTO IapCounselorNotes (NoteId, PlanId, Author, NoteDate, Content)
                        VALUES
                        ('n1', 'IAP-2026-001', 'David Taylor (Counselor)', '2026-08-05 10:30', 'Student attended intake session receptive to guidance. Identified peer provocation triggers.'),
                        ('n2', 'IAP-2026-001', 'David Taylor (Counselor)', '2026-08-18 14:15', 'Completed counseling Module 1. Juan demonstrated deep breathing and escalation pause techniques.');
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

        public async Task<List<IndividualActionPlan>> GetAllPlansAsync()
        {
            EnsureTables();
            var plans = new List<IndividualActionPlan>();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, StudentId, StudentName, GradeSection, FocusArea, AssignedCounselor, RiskTier, StartDate, ReviewDate, ProblemStatement, CoreRoadmapObjectives, Status, CreatedAt FROM IndividualActionPlans ORDER BY CreatedAt DESC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var plan = new IndividualActionPlan
                {
                    Id = reader.GetString(0),
                    StudentId = reader.GetString(1),
                    StudentName = reader.GetString(2),
                    GradeSection = reader.IsDBNull(3) ? "Grade 10" : reader.GetString(3),
                    FocusArea = reader.GetString(4),
                    AssignedCounselor = reader.GetString(5),
                    RiskTier = reader.GetString(6),
                    StartDate = reader.GetString(7),
                    ReviewDate = reader.GetString(8),
                    ProblemStatement = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    CoreRoadmapObjectives = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    Status = reader.GetString(11)
                };
                plans.Add(plan);
            }

            foreach (var plan in plans)
            {
                plan.Milestones = await GetMilestonesForPlanAsync(plan.Id);
                plan.Notes = await GetNotesForPlanAsync(plan.Id);
                plan.Attachments = await GetAttachmentsForPlanAsync(plan.Id);
            }

            return plans;
        }

        public async Task<IndividualActionPlan?> GetPlanByIdAsync(string planId)
        {
            var plans = await GetAllPlansAsync();
            return plans.FirstOrDefault(p => p.Id == planId);
        }

        public async Task InsertPlanAsync(IndividualActionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            EnsureTables();

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO IndividualActionPlans (Id, StudentId, StudentName, GradeSection, FocusArea, AssignedCounselor, RiskTier, StartDate, ReviewDate, ProblemStatement, CoreRoadmapObjectives, Status)
                VALUES (@Id, @StudentId, @StudentName, @GradeSection, @FocusArea, @AssignedCounselor, @RiskTier, @StartDate, @ReviewDate, @ProblemStatement, @CoreRoadmapObjectives, @Status);";

            AddParam(cmd, "@Id", plan.Id);
            AddParam(cmd, "@StudentId", plan.StudentId);
            AddParam(cmd, "@StudentName", plan.StudentName);
            AddParam(cmd, "@GradeSection", plan.GradeSection);
            AddParam(cmd, "@FocusArea", plan.FocusArea);
            AddParam(cmd, "@AssignedCounselor", plan.AssignedCounselor);
            AddParam(cmd, "@RiskTier", plan.RiskTier);
            AddParam(cmd, "@StartDate", plan.StartDate);
            AddParam(cmd, "@ReviewDate", plan.ReviewDate);
            AddParam(cmd, "@ProblemStatement", plan.ProblemStatement);
            AddParam(cmd, "@CoreRoadmapObjectives", plan.CoreRoadmapObjectives);
            AddParam(cmd, "@Status", plan.Status);

            await cmd.ExecuteNonQueryAsync();

            foreach (var m in plan.Milestones)
            {
                await AddMilestoneAsync(m);
            }
        }

        public async Task UpdatePlanAsync(IndividualActionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE IndividualActionPlans 
                SET Status = @Status, FocusArea = @FocusArea, AssignedCounselor = @AssignedCounselor, RiskTier = @RiskTier, ReviewDate = @ReviewDate, ProblemStatement = @ProblemStatement, CoreRoadmapObjectives = @CoreRoadmapObjectives
                WHERE Id = @Id;";

            AddParam(cmd, "@Id", plan.Id);
            AddParam(cmd, "@Status", plan.Status);
            AddParam(cmd, "@FocusArea", plan.FocusArea);
            AddParam(cmd, "@AssignedCounselor", plan.AssignedCounselor);
            AddParam(cmd, "@RiskTier", plan.RiskTier);
            AddParam(cmd, "@ReviewDate", plan.ReviewDate);
            AddParam(cmd, "@ProblemStatement", plan.ProblemStatement);
            AddParam(cmd, "@CoreRoadmapObjectives", plan.CoreRoadmapObjectives);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddMilestoneAsync(IapMilestone milestone)
        {
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO IapMilestones (MilestoneId, PlanId, Title, DueDate, IsCompleted)
                VALUES (@MilestoneId, @PlanId, @Title, @DueDate, @IsCompleted);";

            AddParam(cmd, "@MilestoneId", milestone.MilestoneId);
            AddParam(cmd, "@PlanId", milestone.PlanId);
            AddParam(cmd, "@Title", milestone.Title);
            AddParam(cmd, "@DueDate", milestone.DueDate);
            AddParam(cmd, "@IsCompleted", milestone.IsCompleted ? 1 : 0);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ToggleMilestoneAsync(string milestoneId, bool isCompleted)
        {
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE IapMilestones SET IsCompleted = @IsCompleted WHERE MilestoneId = @MilestoneId;";
            AddParam(cmd, "@MilestoneId", milestoneId);
            AddParam(cmd, "@IsCompleted", isCompleted ? 1 : 0);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddNoteAsync(IapCounselorNote note)
        {
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO IapCounselorNotes (NoteId, PlanId, Author, NoteDate, Content)
                VALUES (@NoteId, @PlanId, @Author, @NoteDate, @Content);";

            AddParam(cmd, "@NoteId", note.NoteId);
            AddParam(cmd, "@PlanId", note.PlanId);
            AddParam(cmd, "@Author", note.Author);
            AddParam(cmd, "@NoteDate", note.NoteDate);
            AddParam(cmd, "@Content", note.Content);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddAttachmentAsync(IapAttachment attachment)
        {
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO IapAttachments (AttachmentId, PlanId, FileName, FilePath, UploadDate)
                VALUES (@AttachmentId, @PlanId, @FileName, @FilePath, @UploadDate);";

            AddParam(cmd, "@AttachmentId", attachment.AttachmentId);
            AddParam(cmd, "@PlanId", attachment.PlanId);
            AddParam(cmd, "@FileName", attachment.FileName);
            AddParam(cmd, "@FilePath", attachment.FilePath);
            AddParam(cmd, "@UploadDate", attachment.UploadDate);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<List<IapMilestone>> GetMilestonesForPlanAsync(string planId)
        {
            var list = new List<IapMilestone>();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT MilestoneId, PlanId, Title, DueDate, IsCompleted FROM IapMilestones WHERE PlanId = @PlanId;";
            AddParam(cmd, "@PlanId", planId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new IapMilestone
                {
                    MilestoneId = reader.GetString(0),
                    PlanId = reader.GetString(1),
                    Title = reader.GetString(2),
                    DueDate = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    IsCompleted = reader.GetInt32(4) == 1
                });
            }
            return list;
        }

        private async Task<List<IapCounselorNote>> GetNotesForPlanAsync(string planId)
        {
            var list = new List<IapCounselorNote>();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT NoteId, PlanId, Author, NoteDate, Content FROM IapCounselorNotes WHERE PlanId = @PlanId ORDER BY NoteDate DESC;";
            AddParam(cmd, "@PlanId", planId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new IapCounselorNote
                {
                    NoteId = reader.GetString(0),
                    PlanId = reader.GetString(1),
                    Author = reader.GetString(2),
                    NoteDate = reader.GetString(3),
                    Content = reader.GetString(4)
                });
            }
            return list;
        }

        private async Task<List<IapAttachment>> GetAttachmentsForPlanAsync(string planId)
        {
            var list = new List<IapAttachment>();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT AttachmentId, PlanId, FileName, FilePath, UploadDate FROM IapAttachments WHERE PlanId = @PlanId;";
            AddParam(cmd, "@PlanId", planId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new IapAttachment
                {
                    AttachmentId = reader.GetString(0),
                    PlanId = reader.GetString(1),
                    FileName = reader.GetString(2),
                    FilePath = reader.GetString(3),
                    UploadDate = reader.GetString(4)
                });
            }
            return list;
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