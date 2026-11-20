using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Models;

namespace SmartSchoolIncidentManagementSystem.Data
{
    /// <summary>
    /// Helper to ensure SQLite database schema contains all legacy and newly introduced tables and columns.
    /// </summary>
    public static class SqliteSchemaMigrator
    {
        public static void EnsureSchemaCreated(AppDbContext context)
        {
            try
            {
                // Students table migrations
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""Adviser"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""GuardianName"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""GuardianContact"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""MiddleName"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""Gender"" TEXT NOT NULL DEFAULT 'Male';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""DateOfBirth"" TEXT NULL;");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""Email"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""Phone"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""Address"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""TrackStrand"" TEXT NOT NULL DEFAULT 'Academic';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""GuardianRelation"" TEXT NOT NULL DEFAULT 'Parent';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""GuardianEmail"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""MedicalNotes"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Students"" ADD COLUMN ""ProfilePicturePath"" TEXT NOT NULL DEFAULT '';");

                // Incidents table migrations
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""DescriptionEncrypted"" TEXT NULL;");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""Description"" TEXT NULL;");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""CreatedBy"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""InvolvedPersons"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""Witnesses"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""EvidenceData"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""ActionTaken"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""Notes"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""RecommendedAction"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""IsRepeatIncident"" INTEGER NOT NULL DEFAULT 0;");
                TryExec(context, @"ALTER TABLE ""Incidents"" ADD COLUMN ""RepeatIncidentCount"" INTEGER NOT NULL DEFAULT 0;");

                // Investigations table migrations
                TryExec(context, @"ALTER TABLE ""Investigations"" ADD COLUMN ""LeadInvestigatorName"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Investigations"" ADD COLUMN ""InvestigationTimeline"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Investigations"" ADD COLUMN ""IsFollowUpFlagged"" INTEGER NOT NULL DEFAULT 0;");
                TryExec(context, @"ALTER TABLE ""Investigations"" ADD COLUMN ""FollowUpIssueType"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Investigations"" ADD COLUMN ""FollowUpNotes"" TEXT NOT NULL DEFAULT '';");
                TryExec(context, @"ALTER TABLE ""Investigations"" ADD COLUMN ""IsFollowUpResolved"" INTEGER NOT NULL DEFAULT 0;");

                // SuspensionOrders table migrations
                TryExec(context, @"ALTER TABLE ""SuspensionOrders"" ADD COLUMN ""IncidentId"" INTEGER NULL;");
                TryExec(context, @"ALTER TABLE ""SuspensionOrders"" ADD COLUMN ""StudentId"" INTEGER NULL;");
                TryExec(context, @"ALTER TABLE ""SuspensionOrders"" ADD COLUMN ""Reason"" TEXT NOT NULL DEFAULT '';");

                // Create ancillary tables if not exist
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""ProbationRecords"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""RecordNumber"" TEXT NOT NULL DEFAULT '',
                        ""IncidentId"" INTEGER NOT NULL,
                        ""StudentId"" INTEGER NOT NULL,
                        ""StartDate"" TEXT NOT NULL,
                        ""EndDate"" TEXT NOT NULL,
                        ""Reason"" TEXT NOT NULL DEFAULT '',
                        ""ExpectedBehaviorConditions"" TEXT NOT NULL DEFAULT '',
                        ""AssignedStaffId"" INTEGER NULL,
                        ""ApprovedBy"" TEXT NOT NULL DEFAULT '',
                        ""Status"" TEXT NOT NULL DEFAULT 'Active',
                        ""ExtensionReason"" TEXT NOT NULL DEFAULT '',
                        ""ClosedAt"" TEXT NULL,
                        ""OutcomeSummary"" TEXT NOT NULL DEFAULT '',
                        ""SyncId"" TEXT NOT NULL,
                        ""IsSynced"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL,
                        FOREIGN KEY (""IncidentId"") REFERENCES ""Incidents"" (""Id"") ON DELETE RESTRICT,
                        FOREIGN KEY (""StudentId"") REFERENCES ""Students"" (""Id"") ON DELETE RESTRICT,
                        FOREIGN KEY (""AssignedStaffId"") REFERENCES ""StaffMembers"" (""Id"") ON DELETE SET NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProbationRecords_SyncId"" ON ""ProbationRecords"" (""SyncId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ProbationRecords_IncidentId"" ON ""ProbationRecords"" (""IncidentId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ProbationRecords_StudentId"" ON ""ProbationRecords"" (""StudentId"");

                    CREATE TABLE IF NOT EXISTS ""ProbationProgressNotes"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""ProbationRecordId"" INTEGER NOT NULL,
                        ""NoteDate"" TEXT NOT NULL,
                        ""ProgressStatus"" TEXT NOT NULL DEFAULT 'On Track',
                        ""Notes"" TEXT NOT NULL DEFAULT '',
                        ""RecordedBy"" TEXT NOT NULL DEFAULT '',
                        ""SyncId"" TEXT NOT NULL,
                        ""IsSynced"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL,
                        FOREIGN KEY (""ProbationRecordId"") REFERENCES ""ProbationRecords"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProbationProgressNotes_SyncId"" ON ""ProbationProgressNotes"" (""SyncId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ProbationProgressNotes_ProbationRecordId"" ON ""ProbationProgressNotes"" (""ProbationRecordId"");

                    CREATE TABLE IF NOT EXISTS ""StudentMeritLogs"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""MeritId"" TEXT NOT NULL DEFAULT '',
                        ""StudentIdNumber"" TEXT NOT NULL DEFAULT '',
                        ""StudentName"" TEXT NOT NULL DEFAULT '',
                        ""CommendationType"" TEXT NOT NULL DEFAULT '',
                        ""PointsAwarded"" INTEGER NOT NULL DEFAULT 5,
                        ""DateAwarded"" TEXT NOT NULL,
                        ""AwardedBy"" TEXT NOT NULL DEFAULT '',
                        ""Citation"" TEXT NOT NULL DEFAULT '',
                        ""SyncId"" TEXT NOT NULL,
                        ""IsSynced"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_StudentMeritLogs_SyncId"" ON ""StudentMeritLogs"" (""SyncId"");

                    CREATE TABLE IF NOT EXISTS ""KeywordSensitivityRules"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""RuleId"" TEXT NOT NULL DEFAULT '',
                        ""Keyword"" TEXT NOT NULL DEFAULT '',
                        ""Category"" TEXT NOT NULL DEFAULT '',
                        ""Severity"" TEXT NOT NULL DEFAULT '',
                        ""WeightScore"" INTEGER NOT NULL DEFAULT 0,
                        ""MatchType"" TEXT NOT NULL DEFAULT '',
                        ""AutoEscalate"" INTEGER NOT NULL DEFAULT 0,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""SyncId"" TEXT NOT NULL,
                        ""IsSynced"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_KeywordSensitivityRules_SyncId"" ON ""KeywordSensitivityRules"" (""SyncId"");

                    CREATE TABLE IF NOT EXISTS ""LocationWeightRules"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""LocationCode"" TEXT NOT NULL DEFAULT '',
                        ""LocationName"" TEXT NOT NULL DEFAULT '',
                        ""Building"" TEXT NOT NULL DEFAULT '',
                        ""Category"" TEXT NOT NULL DEFAULT '',
                        ""SupervisionLevel"" TEXT NOT NULL DEFAULT '',
                        ""HasCCTV"" INTEGER NOT NULL DEFAULT 0,
                        ""RiskMultiplier"" REAL NOT NULL DEFAULT 1.0,
                        ""IsRestricted"" INTEGER NOT NULL DEFAULT 0,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""SyncId"" TEXT NOT NULL,
                        ""IsSynced"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_LocationWeightRules_SyncId"" ON ""LocationWeightRules"" (""SyncId"");

                    CREATE TABLE IF NOT EXISTS ""RepeatOffenderRules"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""PolicyId"" TEXT NOT NULL DEFAULT '',
                        ""PolicyName"" TEXT NOT NULL DEFAULT '',
                        ""Category"" TEXT NOT NULL DEFAULT '',
                        ""LookbackDays"" INTEGER NOT NULL DEFAULT 30,
                        ""ThresholdCount"" INTEGER NOT NULL DEFAULT 2,
                        ""PenaltyType"" TEXT NOT NULL DEFAULT '',
                        ""BonusPoints"" INTEGER NOT NULL DEFAULT 0,
                        ""AutoSummons"" INTEGER NOT NULL DEFAULT 0,
                        ""FlagProfile"" INTEGER NOT NULL DEFAULT 0,
                        ""RequireDeanSignoff"" INTEGER NOT NULL DEFAULT 0,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""SyncId"" TEXT NOT NULL,
                        ""IsSynced"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RepeatOffenderRules_SyncId"" ON ""RepeatOffenderRules"" (""SyncId"");
                ");
            }
            catch { /* Ignore if already created */ }
        }

        private static void TryExec(AppDbContext context, string sql)
        {
            try
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Column already exists or table not ready
            }
        }
    }
}
