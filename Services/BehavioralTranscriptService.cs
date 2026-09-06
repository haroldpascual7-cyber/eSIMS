using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IBehavioralTranscriptService
    {
        Task<StudentTranscriptResult?> GetStudentTranscriptAsync(string studentIdNumber);
        Task<IEnumerable<string>> GetAvailableStudentNamesAsync();
        Task<List<Student>> GetEnrolledStudentsAsync();
        Task AddMeritLogAsync(StudentMeritLog merit);
        Task AddStudentAsync(Student student);
        Task UpdateStudentAsync(Student student);
        Task<string> GenerateMeritIdAsync();
    }

    public class BehavioralTranscriptService : IBehavioralTranscriptService
    {
        private readonly AppDbContext _context;

        public BehavioralTranscriptService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<Student>> GetEnrolledStudentsAsync()
        {
            return await _context.Students
                .AsNoTracking()
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAvailableStudentNamesAsync()
        {
            // Show only students who have actually been registered in the Students database
            return await _context.Students
                .AsNoTracking()
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => $"{s.FirstName} {s.LastName} ({s.StudentNumber})")
                .ToListAsync();
        }

        public async Task<StudentTranscriptResult?> GetStudentTranscriptAsync(string studentIdNumber)
        {
            if (string.IsNullOrWhiteSpace(studentIdNumber)) return null;

            string clean = studentIdNumber.Trim();
            string idPart = string.Empty;
            string namePart = clean;

            if (clean.Contains('(') && clean.Contains(')'))
            {
                int start = clean.IndexOf('(');
                int end = clean.IndexOf(')', start);
                if (start >= 0 && end > start)
                {
                    namePart = clean.Substring(0, start).Trim();
                    idPart = clean.Substring(start + 1, end - start - 1).Trim();
                }
            }

            var studentEntity = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s =>
                (!string.IsNullOrEmpty(idPart) && s.StudentNumber.ToLower() == idPart.ToLower()) ||
                s.StudentNumber.ToLower() == clean.ToLower() ||
                (s.FirstName + " " + s.LastName).ToLower() == clean.ToLower() ||
                (s.FirstName + " " + s.LastName).ToLower() == namePart.ToLower() ||
                s.StudentNumber.ToLower().Contains(clean.ToLower()) ||
                (s.FirstName + " " + s.LastName).ToLower().Contains(clean.ToLower()));

            // If the student has not been added to the system, return null (do not show fake profiles)
            if (studentEntity == null)
            {
                return null;
            }

            var entries = new List<TranscriptEntryModel>();
            string studentNum = studentEntity.StudentNumber.ToLower();
            string studentFull = studentEntity.FullName.ToLower();

            // --- Incidents ---
            var incidents = await _context.Incidents
                .AsNoTracking()
                .Where(i => i.PrimaryInvolvedStudent != null &&
                            (i.PrimaryInvolvedStudent.ToLower().Contains(studentNum) ||
                             i.PrimaryInvolvedStudent.ToLower().Contains(studentFull)))
                .OrderByDescending(i => i.IncidentDate).ToListAsync();

            int incidentCount = incidents.Count;
            foreach (var inc in incidents)
            {
                int pts = inc.Severity switch
                {
                    IncidentSeverity.Low      => -5,
                    IncidentSeverity.Medium   => -10,
                    IncidentSeverity.High     => -20,
                    IncidentSeverity.Critical => -30,
                    _ => -5
                };
                entries.Add(new TranscriptEntryModel
                {
                    EntryDate      = inc.IncidentDate,
                    SourceModule   = "Incident",
                    ReferenceCode  = inc.IncidentNumber,
                    Title          = inc.Title,
                    ScoreImpact    = pts,
                    HandledBy      = inc.ReportingStaffTeacher,
                    ComplianceStatus = inc.Status == IncidentStatus.Resolved ? "Cleared" : "Active"
                });
            }

            // --- Consequences ---
            var consequences = await _context.ConsequenceLogs
                .AsNoTracking()
                .Where(c => (c.StudentIdNumber != null && c.StudentIdNumber.ToLower() == studentNum) ||
                            (c.StudentName != null && c.StudentName.ToLower().Contains(studentFull)))
                .OrderByDescending(c => c.DateIssued).ToListAsync();

            int sanctionCount = consequences.Count;
            foreach (var csq in consequences)
            {
                entries.Add(new TranscriptEntryModel
                {
                    EntryDate      = csq.DateIssued,
                    SourceModule   = "Consequence",
                    ReferenceCode  = csq.LogId,
                    Title          = csq.ConsequenceType,
                    ScoreImpact    = -10,
                    HandledBy      = csq.IssuedBy,
                    ComplianceStatus = csq.Status == "Completed" ? "Completed" : "Active"
                });
            }

            // --- Restorative Plans ---
            var plans = await _context.RestorativePlans
                .AsNoTracking()
                .Where(p => (p.StudentIdNumber != null && p.StudentIdNumber.ToLower() == studentNum) ||
                            (p.StudentName != null && p.StudentName.ToLower().Contains(studentFull)))
                .OrderByDescending(p => p.StartDate).ToListAsync();

            int interventionCount = plans.Count;
            foreach (var plan in plans)
            {
                entries.Add(new TranscriptEntryModel
                {
                    EntryDate      = plan.StartDate,
                    SourceModule   = "Restorative",
                    ReferenceCode  = plan.PlanId,
                    Title          = plan.PlanType,
                    ScoreImpact    = 0,
                    HandledBy      = plan.AssignedStaff,
                    ComplianceStatus = plan.IsCleared ? "Completed" : "Active"
                });
            }

            // --- Suspension Orders ---
            var suspensions = await _context.SuspensionOrders
                .AsNoTracking()
                .Where(s => (s.StudentIdNumber != null && s.StudentIdNumber.ToLower() == studentNum) ||
                            (s.StudentName != null && s.StudentName.ToLower().Contains(studentFull)))
                .OrderByDescending(s => s.StartDate).ToListAsync();

            int activeSuspensions = suspensions.Count(s => s.Status == "Active Ban");
            foreach (var sus in suspensions)
            {
                entries.Add(new TranscriptEntryModel
                {
                    EntryDate      = sus.StartDate,
                    SourceModule   = "Suspension",
                    ReferenceCode  = sus.OrderId,
                    Title          = $"{sus.SuspensionType} ({sus.TotalDays} days)",
                    ScoreImpact    = -20,
                    HandledBy      = sus.IssuedBy,
                    ComplianceStatus = sus.IsCleared ? "Cleared" : "Active"
                });
            }

            // --- Merit Logs ---
            var merits = await _context.StudentMeritLogs
                .AsNoTracking()
                .Where(m => (m.StudentIdNumber != null && m.StudentIdNumber.ToLower() == studentNum) ||
                            (m.StudentName != null && m.StudentName.ToLower().Contains(studentFull)))
                .OrderByDescending(m => m.DateAwarded).ToListAsync();

            foreach (var merit in merits)
            {
                entries.Add(new TranscriptEntryModel
                {
                    EntryDate      = merit.DateAwarded,
                    SourceModule   = "Merit Award",
                    ReferenceCode  = merit.MeritId,
                    Title          = merit.CommendationType + " – " + merit.Citation,
                    ScoreImpact    = merit.PointsAwarded,
                    HandledBy      = merit.AwardedBy,
                    ComplianceStatus = "Completed"
                });
            }

            int totalPenalties  = Math.Abs(entries.Where(e => e.ScoreImpact < 0).Sum(e => e.ScoreImpact));
            int totalMerits     = entries.Where(e => e.ScoreImpact > 0).Sum(e => e.ScoreImpact);
            int conductScore    = Math.Max(0, Math.Min(100, 100 - totalPenalties + totalMerits));
            int unfulfilledCsq  = consequences.Count(c => c.Status != "Completed");
            bool isEligible     = activeSuspensions == 0 && unfulfilledCsq == 0 && conductScore >= 75;

            return new StudentTranscriptResult
            {
                StudentId         = studentEntity.StudentNumber,
                StudentName       = studentEntity.FullName,
                FirstName         = studentEntity.FirstName,
                MiddleName        = studentEntity.MiddleName ?? string.Empty,
                LastName          = studentEntity.LastName,
                Gender            = !string.IsNullOrWhiteSpace(studentEntity.Gender) ? studentEntity.Gender : "Unspecified",
                DateOfBirth       = studentEntity.DateOfBirth,
                Email             = studentEntity.Email ?? string.Empty,
                Phone             = studentEntity.Phone ?? string.Empty,
                Address           = studentEntity.Address ?? string.Empty,
                GradeLevel        = !string.IsNullOrWhiteSpace(studentEntity.GradeLevel) ? studentEntity.GradeLevel : "Unassigned",
                Section           = !string.IsNullOrWhiteSpace(studentEntity.Section)
                                      ? $"{studentEntity.GradeLevel} - {studentEntity.Section}"
                                      : (studentEntity.GradeLevel ?? "Unassigned"),
                TrackStrand       = !string.IsNullOrWhiteSpace(studentEntity.TrackStrand) ? studentEntity.TrackStrand : "Academic",
                Adviser           = !string.IsNullOrWhiteSpace(studentEntity.Adviser) ? studentEntity.Adviser : "Unassigned",
                GuardianName      = !string.IsNullOrWhiteSpace(studentEntity.GuardianName) ? studentEntity.GuardianName : "None Listed",
                GuardianRelation  = !string.IsNullOrWhiteSpace(studentEntity.GuardianRelation) ? studentEntity.GuardianRelation : "Guardian",
                GuardianContact   = !string.IsNullOrWhiteSpace(studentEntity.GuardianContact) ? studentEntity.GuardianContact : "None Provided",
                GuardianEmail     = studentEntity.GuardianEmail ?? string.Empty,
                MedicalNotes      = studentEntity.MedicalNotes ?? string.Empty,
                ProfilePicturePath= studentEntity.ProfilePicturePath ?? string.Empty,
                Entries           = entries.OrderByDescending(e => e.EntryDate).ToList(),
                ConductScore      = conductScore,
                IsEligible        = isEligible,
                IncidentCount     = incidentCount,
                SanctionCount     = sanctionCount,
                InterventionCount = interventionCount,
                ActiveSuspensionCount = activeSuspensions
            };
        }

        public async Task AddMeritLogAsync(StudentMeritLog merit)
        {
            if (merit == null) throw new ArgumentNullException(nameof(merit));
            if (string.IsNullOrWhiteSpace(merit.MeritId)) merit.MeritId = await GenerateMeritIdAsync();
            merit.CreatedAt = DateTime.UtcNow;
            merit.UpdatedAt = DateTime.UtcNow;
            await _context.StudentMeritLogs.AddAsync(merit);
            await _context.SaveChangesAsync();
        }

        public async Task AddStudentAsync(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            student.CreatedAt = DateTime.UtcNow;
            student.UpdatedAt = DateTime.UtcNow;
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStudentAsync(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            var existing = await _context.Students.FirstOrDefaultAsync(s => s.StudentNumber == student.StudentNumber || s.Id == student.Id);
            if (existing != null)
            {
                existing.FirstName          = student.FirstName;
                existing.MiddleName         = student.MiddleName;
                existing.LastName           = student.LastName;
                existing.Gender             = student.Gender;
                existing.DateOfBirth        = student.DateOfBirth;
                existing.Email              = student.Email;
                existing.Phone              = student.Phone;
                existing.Address            = student.Address;
                existing.GradeLevel         = student.GradeLevel;
                existing.Section            = student.Section;
                existing.TrackStrand        = student.TrackStrand;
                existing.Adviser            = student.Adviser;
                existing.GuardianName       = student.GuardianName;
                existing.GuardianRelation   = student.GuardianRelation;
                existing.GuardianContact    = student.GuardianContact;
                existing.GuardianEmail       = student.GuardianEmail;
                existing.MedicalNotes       = student.MedicalNotes;
                existing.ProfilePicturePath = student.ProfilePicturePath;
                existing.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateMeritIdAsync()
        {
            int count = await _context.StudentMeritLogs.CountAsync() + 1;
            return $"MER-{DateTime.Now.Year}-{count:D4}";
        }
    }
}