using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Linq;

namespace SmartSchoolIncidentManagementSystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();
            EnsureTablesCreated(context);

            // 0. System Settings
            if (!context.SystemSettings.Any())
            {
                context.SystemSettings.AddRange(new SystemSetting[]
                {
                    new SystemSetting { Key = "SchoolName", Value = "Smart Academy", Description = "The name of the educational institution" },
                    new SystemSetting { Key = "SystemTitle", Value = "Incident Management System", Description = "Application display title" },
                    new SystemSetting { Key = "Tagline", Value = "Fostering a safe, accountable, and supportive educational environment.", Description = "App sub-heading/tagline" },
                    new SystemSetting { Key = "Motto", Value = "Safe Campus • Real-time Reporting • Smart Resolution", Description = "Campus safety motto" },
                    new SystemSetting { Key = "LogoIcon", Value = "🛡️", Description = "Icon or logo glyph" }
                });
                context.SaveChanges();
            }

            // 1. Seed Students
            if (!context.Students.Any())
            {
                context.Students.AddRange(new Student[]
                {
                    new Student { StudentNumber = "STD-1042", FirstName = "Juan", MiddleName = "Reyes", LastName = "Dela Cruz", Gender = "Male", DateOfBirth = new DateTime(2009, 10, 12), Email = "juan.delacruz@smartschool.edu", Phone = "0917-123-4567", Address = "123 Mabini St., Quezon City", GradeLevel = "Grade 10", Section = "St. Thomas", TrackStrand = "Academic", Adviser = "Mr. Carlo Reyes", GuardianName = "Maria Dela Cruz", GuardianRelation = "Mother", GuardianContact = "0917-555-1234", GuardianEmail = "maria.delacruz@gmail.com", MedicalNotes = "No severe allergies reported." },
                    new Student { StudentNumber = "STD-1001", FirstName = "John", LastName = "Doe", GradeLevel = "Grade 10", Section = "A", Adviser = "James Miller", GuardianName = "Robert Doe", GuardianContact = "0917-111-2222" },
                    new Student { StudentNumber = "STD-1002", FirstName = "Jane", LastName = "Smith", GradeLevel = "Grade 10", Section = "B", Adviser = "Linda Wilson", GuardianName = "Sarah Smith", GuardianContact = "0917-333-4444" },
                    new Student { StudentNumber = "STD-1003", FirstName = "Robert", LastName = "Johnson", GradeLevel = "Grade 11", Section = "A", Adviser = "David Taylor", GuardianName = "Mary Johnson", GuardianContact = "0917-555-6666" },
                    new Student { StudentNumber = "STD-1004", FirstName = "Emily", LastName = "Williams", GradeLevel = "Grade 9", Section = "C", Adviser = "Sarah Davis", GuardianName = "James Williams", GuardianContact = "0917-777-8888" },
                    new Student { StudentNumber = "STD-1005", FirstName = "Michael", LastName = "Brown", GradeLevel = "Grade 12", Section = "A", Adviser = "James Miller", GuardianName = "Patricia Brown", GuardianContact = "0917-999-0000" }
                });
                context.SaveChanges();
            }

            // 2. Seed Staff
            if (!context.StaffMembers.Any())
            {
                context.StaffMembers.AddRange(new Staff[]
                {
                    new Staff { StaffNumber = "STF001", FirstName = "Sarah", LastName = "Davis", Department = "Administration", Role = "Principal" },
                    new Staff { StaffNumber = "STF002", FirstName = "James", LastName = "Miller", Department = "Science", Role = "Teacher" },
                    new Staff { StaffNumber = "STF003", FirstName = "Linda", LastName = "Wilson", Department = "Mathematics", Role = "Teacher" },
                    new Staff { StaffNumber = "STF004", FirstName = "David", LastName = "Taylor", Department = "Discipline", Role = "Guidance Counselor" }
                });
                context.SaveChanges();
            }

            // 3. Seed Users
            if (!context.Users.Any())
            {
                context.Users.AddRange(new User[]
                {
                    new User { Username = "Admin", PasswordHash = "Admin123", Email = "Admin", Role = "Administrator" },
                    new User { Username = "staff", PasswordHash = "staff123", Email = "staff", Role = "Staff" },
                    new User { Username = "discipline_lead", PasswordHash = "discipline123", Email = "discipline_lead", Role = "Discipline Officer" },
                    new User { Username = "guidance_counselor", PasswordHash = "guidance123", Email = "guidance_counselor", Role = "Guidance Counselor" },
                    new User { Username = "admin", PasswordHash = "Admin123", Email = "admin@smartschool.edu", Role = "Administrator" },
                    new User { Username = "teacher", PasswordHash = "teacher123", Email = "teacher@smartschool.edu", Role = "Teacher" }
                });
                context.SaveChanges();
            }
            else
            {
                EnsureDemoUser(context, "Admin", "Admin123", "Admin", "Administrator");
                EnsureDemoUser(context, "staff", "staff123", "staff", "Staff");
                EnsureDemoUser(context, "discipline_lead", "discipline123", "discipline_lead", "Discipline Officer");
                EnsureDemoUser(context, "guidance_counselor", "guidance123", "guidance_counselor", "Guidance Counselor");
                EnsureDemoUser(context, "admin", "Admin123", "admin@smartschool.edu", "Administrator");
                EnsureDemoUser(context, "guidance", "guidance123", "guidance@smartschool.edu", "Guidance Counselor");
                EnsureDemoUser(context, "discipline", "discipline123", "discipline@smartschool.edu", "Discipline Officer");
                EnsureDemoUser(context, "teacher", "teacher123", "teacher@smartschool.edu", "Teacher");
                context.SaveChanges();
            }

            // 4. Seed Incidents
            if (!context.Incidents.Any())
            {
                context.Incidents.AddRange(new Incident[]
                {
                    new Incident
                    {
                        IncidentNumber = "INC-2026-00001",
                        Title = "Classroom Disruption",
                        Category = "Classroom Disruption",
                        Description = "Student was repeatedly speaking out of turn and throwing paper airplanes during chemistry class.",
                        Location = "Room 302 - Science Lab",
                        IncidentDate = DateTime.Now.AddDays(-20),
                        PrimaryInvolvedStudent = "John Doe (STD-1001)",
                        ReportingStaffTeacher = "James Miller",
                        Status = IncidentStatus.Resolved,
                        Severity = IncidentSeverity.Low,
                        Risk = RiskLevel.Low,
                        RecommendedAction = "Verbal reprimand, teacher-student counseling, and 14-day behavior monitoring.",
                        ActionTaken = "Verbal warning given, teacher spoke with student after class.",
                        Notes = "Student apologized and promised not to repeat the behavior."
                    },
                    new Incident
                    {
                        IncidentNumber = "INC-2026-00002",
                        Title = "Skipping Detention",
                        Category = "Truancy",
                        Description = "Student did not show up for scheduled after-school detention without prior excuse.",
                        Location = "Detention Hall",
                        IncidentDate = DateTime.Now.AddDays(-14),
                        PrimaryInvolvedStudent = "Jane Smith (STD-1002)",
                        ReportingStaffTeacher = "David Taylor",
                        Status = IncidentStatus.UnderInvestigation,
                        Severity = IncidentSeverity.Medium,
                        Risk = RiskLevel.Medium,
                        RecommendedAction = "Issue written reprimand, teacher conference, and parent notification.",
                        ActionTaken = "Parent contacted. Scheduled a meeting with guidance counselor.",
                        Notes = "Meeting scheduled for Monday morning."
                    },
                    new Incident
                    {
                        IncidentNumber = "INC-2026-00003",
                        Title = "Vandalism on Lockers",
                        Category = "Vandalism",
                        Description = "Graffiti drawn with permanent marker on locker numbers 124 to 128.",
                        Location = "Locker Area B",
                        IncidentDate = DateTime.Now.AddDays(-10),
                        PrimaryInvolvedStudent = "Robert Johnson (STD-1003)",
                        ReportingStaffTeacher = "Sarah Davis",
                        Status = IncidentStatus.Reported,
                        Severity = IncidentSeverity.High,
                        Risk = RiskLevel.High,
                        RecommendedAction = "Initiate formal incident investigation, parent notification, and guidance counselor evaluation.",
                        ActionTaken = "CCTV footage reviewed. Identified the student. Lockers were cleaned by facilities.",
                        Notes = "Disciplinary hearing to be scheduled. Parents notified."
                    },
                    new Incident
                    {
                        IncidentNumber = "INC-2026-00004",
                        Title = "Physical Altercation on Playground",
                        Category = "Physical Altercation",
                        Description = "Engaged in a physical scuffle on the playground during lunch break over a sports dispute.",
                        Location = "Playground",
                        IncidentDate = DateTime.Now.AddDays(-5),
                        PrimaryInvolvedStudent = "Juan Dela Cruz (STD-1042)",
                        ReportingStaffTeacher = "David Taylor",
                        Status = IncidentStatus.UnderInvestigation,
                        Severity = IncidentSeverity.Critical,
                        Risk = RiskLevel.Critical,
                        RecommendedAction = "IMMEDIATE ACTION: Escalate to School Principal & Security Officer immediately.",
                        ActionTaken = "Separated immediately by supervisors. Sent to principal's office. Parents summoned.",
                        Notes = "Suspension being considered. Waiting for medical check on minor bruises."
                    },
                    new Incident
                    {
                        IncidentNumber = "INC-2026-00005",
                        Title = "Habitual Tardiness & Late Arrival",
                        Category = "Truancy",
                        Description = "Arrived 25 minutes late to first period class three times in a single week without excuse.",
                        Location = "Main Gate / Classroom 201",
                        IncidentDate = DateTime.Now.AddDays(-15),
                        PrimaryInvolvedStudent = "Juan Dela Cruz (STD-1042)",
                        ReportingStaffTeacher = "Mr. Carlo Reyes",
                        Status = IncidentStatus.Resolved,
                        Severity = IncidentSeverity.Low,
                        Risk = RiskLevel.Low,
                        RecommendedAction = "Verbal reprimand and morning check-in contract.",
                        ActionTaken = "Advised on time management. Parent notified.",
                        Notes = "Agreed to morning sign-in slip."
                    },
                    new Incident
                    {
                        IncidentNumber = "INC-2026-00006",
                        Title = "Disrespectful Language to Faculty",
                        Category = "Classroom Disruption",
                        Description = "Used inappropriate language when requested to put away mobile phone during lecture.",
                        Location = "Room 204 - English Wing",
                        IncidentDate = DateTime.Now.AddDays(-8),
                        PrimaryInvolvedStudent = "Juan Dela Cruz (STD-1042)",
                        ReportingStaffTeacher = "Mrs. Ana Santos",
                        Status = IncidentStatus.Resolved,
                        Severity = IncidentSeverity.Medium,
                        Risk = RiskLevel.Medium,
                        RecommendedAction = "Apology letter and guidance intake counseling.",
                        ActionTaken = "Written apology submitted and counseling intake logged.",
                        Notes = "Student acknowledged error and cleared by counselor."
                    }
                });
                context.SaveChanges();
            }

            // 5. Seed Consequence Logs
            if (!context.ConsequenceLogs.Any())
            {
                context.ConsequenceLogs.AddRange(new ConsequenceLog[]
                {
                    new ConsequenceLog { LogId = "CSQ-2026-001", IncidentNumber = "INC-2026-00001", StudentIdNumber = "STD-1001", StudentName = "John Doe", ConsequenceType = "Verbal Warning", OffenseLevel = "1st Offense", DateIssued = DateTime.Now.AddDays(-20), DueDate = DateTime.Now.AddDays(-18), IssuedBy = "admin", ParentNotified = true, ParentSigned = true, Status = "Completed", SanctionTerms = "Verbal warning issued regarding classroom disruption." },
                    new ConsequenceLog { LogId = "CSQ-2026-002", IncidentNumber = "INC-2026-00002", StudentIdNumber = "STD-1002", StudentName = "Jane Smith", ConsequenceType = "Written Reprimand", OffenseLevel = "2nd Offense", DateIssued = DateTime.Now.AddDays(-14), DueDate = DateTime.Now.AddDays(-10), IssuedBy = "admin", ParentNotified = true, ParentSigned = false, Status = "Scheduled", SanctionTerms = "Official written reprimand for unexcused absence from detention hall." },
                    new ConsequenceLog { LogId = "CSQ-2026-003", IncidentNumber = "INC-2026-00004", StudentIdNumber = "STD-1042", StudentName = "Juan Dela Cruz", ConsequenceType = "Parent Conference & Detention", OffenseLevel = "3rd Offense / Critical", DateIssued = DateTime.Now.AddDays(-4), DueDate = DateTime.Now.AddDays(-2), IssuedBy = "admin", ParentNotified = true, ParentSigned = false, Status = "Overdue", SanctionTerms = "Mandatory parent conference required following physical altercation on the playground." }
                });
                context.SaveChanges();
            }

            // 6. Seed Restorative Plans
            if (!context.RestorativePlans.Any())
            {
                context.RestorativePlans.AddRange(new RestorativePlan[]
                {
                    new RestorativePlan { PlanId = "INT-2026-001", IncidentNumber = "INC-2026-00001", StudentIdNumber = "STD-1001", StudentName = "John Doe", PlanType = "Community Service", AssignedStaff = "Facilities & Grounds", TargetMetric = 10, CompletedMetric = 6, StartDate = DateTime.Now.AddDays(-18), DeadlineDate = DateTime.Now.AddDays(5), Objectives = "Campus library assistance.", Status = "Active", IsCleared = false },
                    new RestorativePlan { PlanId = "INT-2026-002", IncidentNumber = "INC-2026-00002", StudentIdNumber = "STD-1002", StudentName = "Jane Smith", PlanType = "Individual Counseling", AssignedStaff = "David Taylor (Guidance)", TargetMetric = 4, CompletedMetric = 2, StartDate = DateTime.Now.AddDays(-12), DeadlineDate = DateTime.Now.AddDays(7), Objectives = "Bi-weekly punctuality counseling.", Status = "Active", IsCleared = false },
                    new RestorativePlan { PlanId = "INT-2026-003", IncidentNumber = "INC-2026-00004", StudentIdNumber = "STD-1042", StudentName = "Juan Dela Cruz", PlanType = "Apology Letter & Peer Mediation", AssignedStaff = "Sarah Davis (Principal)", TargetMetric = 5, CompletedMetric = 5, StartDate = DateTime.Now.AddDays(-10), DeadlineDate = DateTime.Now.AddDays(-2), Objectives = "Peer mediation agreement.", Status = "Completed", IsCleared = true }
                });
                context.SaveChanges();
            }

            // 7. Seed Suspension Orders
            if (!context.SuspensionOrders.Any())
            {
                context.SuspensionOrders.AddRange(new SuspensionOrder[]
                {
                    new SuspensionOrder { OrderId = "SUS-2026-001", IncidentNumber = "INC-2026-00004", StudentIdNumber = "STD-1042", StudentName = "Juan Dela Cruz", SuspensionType = "Out-of-School Suspension (OSS)", StartDate = DateTime.Now.AddDays(-3), EndDate = DateTime.Now.AddDays(2), TotalDays = 5, ReentryDate = DateTime.Now.AddDays(3), IssuedBy = "Dean of Student Affairs", Terms = "Campus ban parameters enforced.", ParentConferenceRequired = true, AcademicPacketRequired = true, CounselorIntakeRequired = true, ParentConferencePassed = false, AcademicPacketPassed = true, CounselorIntakePassed = false, IsCleared = false, Status = "Active Ban" },
                    new SuspensionOrder { OrderId = "SUS-2026-002", IncidentNumber = "INC-2026-00002", StudentIdNumber = "STD-1002", StudentName = "Jane Smith", SuspensionType = "In-School Suspension (ISS)", StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(-1), TotalDays = 4, ReentryDate = DateTime.Now, IssuedBy = "Prefect of Discipline", Terms = "Assigned to ISS Hall A-10.", ParentConferenceRequired = true, AcademicPacketRequired = true, CounselorIntakeRequired = true, ParentConferencePassed = true, AcademicPacketPassed = true, CounselorIntakePassed = false, IsCleared = false, Status = "Pending Review" },
                    new SuspensionOrder { OrderId = "SUS-2026-003", IncidentNumber = "INC-2026-00001", StudentIdNumber = "STD-1001", StudentName = "John Doe", SuspensionType = "In-School Suspension (ISS)", StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(-7), TotalDays = 3, ReentryDate = DateTime.Now.AddDays(-6), IssuedBy = "Prefect of Discipline", Terms = "Behavior reflection packet completed.", ParentConferenceRequired = true, AcademicPacketRequired = true, CounselorIntakeRequired = true, ParentConferencePassed = true, AcademicPacketPassed = true, CounselorIntakePassed = true, IsCleared = true, Status = "Readmitted" }
                });
                context.SaveChanges();
            }

            // 8. Seed Keyword Sensitivity Rules
            if (!context.KeywordSensitivityRules.Any())
            {
                context.KeywordSensitivityRules.AddRange(new KeywordSensitivityRule[]
                {
                    new KeywordSensitivityRule { RuleId = "KWD-001", Keyword = "switchblade", Category = "Weapons & Threats", Severity = "Critical", WeightScore = 90, MatchType = "Contains Word Root", AutoEscalate = true, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-002", Keyword = "physical assault", Category = "Violence & Altercation", Severity = "Critical", WeightScore = 85, MatchType = "Contains Word Root", AutoEscalate = true, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-003", Keyword = "vape", Category = "Substance & Contraband", Severity = "Medium", WeightScore = 40, MatchType = "Contains Word Root", AutoEscalate = false, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-004", Keyword = "extortion", Category = "Bullying & Harassment", Severity = "High", WeightScore = 70, MatchType = "Contains Word Root", AutoEscalate = true, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-005", Keyword = "vandalism", Category = "Vandalism & Property", Severity = "Medium", WeightScore = 35, MatchType = "Contains Word Root", AutoEscalate = false, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-006", Keyword = "cheating", Category = "Academic & Protocol", Severity = "Low", WeightScore = 15, MatchType = "Contains Word Root", AutoEscalate = false, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-007", Keyword = "knife", Category = "Weapons & Threats", Severity = "Critical", WeightScore = 95, MatchType = "Contains Word Root", AutoEscalate = true, IsActive = true },
                    new KeywordSensitivityRule { RuleId = "KWD-008", Keyword = "altercation", Category = "Violence & Altercation", Severity = "Medium", WeightScore = 30, MatchType = "Contains Word Root", AutoEscalate = false, IsActive = true }
                });
                context.SaveChanges();
            }

            // 9. Seed Location Weight Rules
            if (!context.LocationWeightRules.Any())
            {
                context.LocationWeightRules.AddRange(new LocationWeightRule[]
                {
                    new LocationWeightRule { LocationCode = "LOC-001", LocationName = "Back Gymnasium Bleachers", Building = "Sports Complex", Category = "Gym/Field", SupervisionLevel = "Unmonitored Blind Spot", HasCCTV = false, RiskMultiplier = 1.8, IsRestricted = true, IsActive = true },
                    new LocationWeightRule { LocationCode = "LOC-002", LocationName = "Restrooms - East Wing 2nd Flr", Building = "Academic Building B", Category = "Restroom/Locker", SupervisionLevel = "Unmonitored Blind Spot", HasCCTV = false, RiskMultiplier = 1.6, IsRestricted = false, IsActive = true },
                    new LocationWeightRule { LocationCode = "LOC-003", LocationName = "Main Library Reading Hall", Building = "Central Building", Category = "Classroom", SupervisionLevel = "High Constant Supervision", HasCCTV = true, RiskMultiplier = 1.0, IsRestricted = false, IsActive = true },
                    new LocationWeightRule { LocationCode = "LOC-004", LocationName = "Student Canteen & Courtyard", Building = "Student Center", Category = "Hallway/Stairwell", SupervisionLevel = "Periodic Patrol", HasCCTV = true, RiskMultiplier = 1.2, IsRestricted = false, IsActive = true },
                    new LocationWeightRule { LocationCode = "LOC-005", LocationName = "Science Chemistry Lab 3", Building = "Science Wing", Category = "Classroom", SupervisionLevel = "High Constant Supervision", HasCCTV = true, RiskMultiplier = 1.3, IsRestricted = true, IsActive = true }
                });
                context.SaveChanges();
            }

            // 10. Seed Repeat Offender Rules
            if (!context.RepeatOffenderRules.Any())
            {
                context.RepeatOffenderRules.AddRange(new RepeatOffenderRule[]
                {
                    new RepeatOffenderRule { PolicyId = "REC-001", PolicyName = "30-Day Violence Escalation Policy", Category = "Violence & Altercation", LookbackDays = 30, ThresholdCount = 2, PenaltyType = "Bump Severity Tier by +1", BonusPoints = 25, AutoSummons = true, FlagProfile = true, RequireDeanSignoff = true, IsActive = true },
                    new RepeatOffenderRule { PolicyId = "REC-002", PolicyName = "45-Day Bullying & Harassment Recidivism", Category = "Bullying & Harassment", LookbackDays = 45, ThresholdCount = 2, PenaltyType = "Add Flat Risk Points", BonusPoints = 30, AutoSummons = true, FlagProfile = true, RequireDeanSignoff = true, IsActive = true },
                    new RepeatOffenderRule { PolicyId = "REC-003", PolicyName = "60-Day Cumulative Repeat Infraction Rule", Category = "Cumulative Infractions", LookbackDays = 60, ThresholdCount = 3, PenaltyType = "Force Critical Tier Immediately", BonusPoints = 40, AutoSummons = true, FlagProfile = true, RequireDeanSignoff = true, IsActive = true }
                });
                context.SaveChanges();
            }

            // 11. Seed Student Merit Logs (Juan Dela Cruz – STD-1042)
            if (!context.StudentMeritLogs.Any())
            {
                context.StudentMeritLogs.AddRange(new StudentMeritLog[]
                {
                    new StudentMeritLog { MeritId = "MER-2026-0001", StudentIdNumber = "STD-1042", StudentName = "Juan Dela Cruz", CommendationType = "Peer Leadership", PointsAwarded = 5, DateAwarded = new DateTime(2026, 2, 10), AwardedBy = "Mr. Carlo Reyes", Citation = "Returned a lost wallet with all contents intact to the school office." },
                    new StudentMeritLog { MeritId = "MER-2026-0002", StudentIdNumber = "STD-1042", StudentName = "Juan Dela Cruz", CommendationType = "Academic Excellence", PointsAwarded = 10, DateAwarded = new DateTime(2026, 3, 18), AwardedBy = "Mrs. Ana Santos", Citation = "Ranked top 10 in the quarterly academic performance assessment." },
                    new StudentMeritLog { MeritId = "MER-2026-0003", StudentIdNumber = "STD-1042", StudentName = "Juan Dela Cruz", CommendationType = "Civic Engagement", PointsAwarded = 5, DateAwarded = new DateTime(2026, 5, 5), AwardedBy = "Mrs. Ana Santos", Citation = "Volunteered and led classmates in organizing the school tree planting drive." }
                });
                context.SaveChanges();
            }
        }

        private static void EnsureTablesCreated(AppDbContext context)
        {
            try
            {
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""Adviser"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""GuardianName"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""GuardianContact"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""MiddleName"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""Gender"" TEXT NOT NULL DEFAULT 'Male';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""DateOfBirth"" TEXT NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""Email"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""Phone"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""Address"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""TrackStrand"" TEXT NOT NULL DEFAULT 'Academic';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""GuardianRelation"" TEXT NOT NULL DEFAULT 'Parent';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""GuardianEmail"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""MedicalNotes"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Students"" ADD COLUMN ""ProfilePicturePath"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Incidents"" ADD COLUMN ""CreatedBy"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Incidents"" ADD COLUMN ""InvolvedPersons"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Incidents"" ADD COLUMN ""Witnesses"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Incidents"" ADD COLUMN ""EvidenceData"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Investigations"" ADD COLUMN ""InvestigationTimeline"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Investigations"" ADD COLUMN ""IsFollowUpFlagged"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Investigations"" ADD COLUMN ""FollowUpIssueType"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Investigations"" ADD COLUMN ""FollowUpNotes"" TEXT NOT NULL DEFAULT '';"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Investigations"" ADD COLUMN ""IsFollowUpResolved"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""SuspensionOrders"" ADD COLUMN ""IncidentId"" INTEGER NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""SuspensionOrders"" ADD COLUMN ""StudentId"" INTEGER NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""SuspensionOrders"" ADD COLUMN ""Reason"" TEXT NOT NULL DEFAULT '';"); } catch { }

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
            catch { /* Ignore if already created or using in-memory */ }
        }

        private static void EnsureDemoUser(AppDbContext context, string username, string password, string email, string role)
        {
            var user = context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                context.Users.Add(new User
                {
                    Username = username,
                    PasswordHash = password,
                    Email = email,
                    Role = role,
                    IsActive = true
                });
            }
            else
            {
                user.Username = username;
                user.PasswordHash = password;
                user.Email = email;
                user.Role = role;
                user.IsActive = true;
            }
        }
    }
}
