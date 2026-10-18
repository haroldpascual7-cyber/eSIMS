using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class ProbationService : IProbationService
    {
        private readonly AppDbContext _context;

        public ProbationService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<ProbationRecord>> GetAllAsync()
        {
            return await _context.ProbationRecords
                .Include(p => p.Incident)
                .Include(p => p.Student)
                .Include(p => p.AssignedStaff)
                .Include(p => p.ProgressNotes)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }

        public async Task AddAsync(ProbationRecord record)
        {
            DisciplinaryAuthorization.DemandManagePermission();
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (record.IncidentId <= 0 || record.StudentId <= 0)
                throw new InvalidOperationException("A verified incident and student are required.");
            if (record.EndDate.Date < record.StartDate.Date)
                throw new InvalidOperationException("The monitoring end date cannot be before the start date.");

            record.RecordNumber = string.IsNullOrWhiteSpace(record.RecordNumber)
                ? await GenerateRecordNumberAsync()
                : record.RecordNumber;
            record.ApprovedBy = SessionManager.CurrentUsername;
            record.Status = "Active";
            record.CreatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            record.IsSynced = false;
            record.ProgressNotes.Add(CreateSystemNote(record, "Started",
                $"Probation monitoring approved through {record.EndDate:yyyy-MM-dd}."));

            await _context.ProbationRecords.AddAsync(record);
            var incident = await _context.Incidents.FindAsync(record.IncidentId);
            if (incident != null && incident.Status != IncidentStatus.Closed)
            {
                incident.Status = IncidentStatus.ActionTaken;
                incident.MarkUpdated();
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddProgressNoteAsync(int recordId, string status, string notes)
        {
            DisciplinaryAuthorization.DemandManagePermission();
            var record = await RequireOpenRecordAsync(recordId);
            if (string.IsNullOrWhiteSpace(notes))
                throw new InvalidOperationException("Progress notes cannot be empty.");

            record.ProgressNotes.Add(CreateSystemNote(record,
                string.IsNullOrWhiteSpace(status) ? "On Track" : status.Trim(), notes.Trim()));
            record.MarkUpdated();
            await _context.SaveChangesAsync();
        }

        public async Task ExtendAsync(int recordId, DateTime newEndDate, string reason)
        {
            DisciplinaryAuthorization.DemandManagePermission();
            var record = await RequireOpenRecordAsync(recordId);
            if (newEndDate.Date <= record.EndDate.Date)
                throw new InvalidOperationException("The extended date must be later than the current end date.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("An extension reason is required.");

            var previousEnd = record.EndDate;
            record.EndDate = newEndDate.Date;
            record.ExtensionReason = reason.Trim();
            record.Status = "Extended";
            record.ProgressNotes.Add(CreateSystemNote(record, "Extended",
                $"Monitoring extended from {previousEnd:yyyy-MM-dd} to {record.EndDate:yyyy-MM-dd}. Reason: {reason.Trim()}"));
            record.MarkUpdated();
            await _context.SaveChangesAsync();
        }

        public async Task CompleteAsync(int recordId, string outcomeSummary)
        {
            DisciplinaryAuthorization.DemandManagePermission();
            var record = await RequireOpenRecordAsync(recordId);
            if (string.IsNullOrWhiteSpace(outcomeSummary))
                throw new InvalidOperationException("A completion outcome is required.");

            record.Status = "Completed";
            record.ClosedAt = DateTime.Now;
            record.OutcomeSummary = outcomeSummary.Trim();
            record.ProgressNotes.Add(CreateSystemNote(record, "Completed", outcomeSummary.Trim()));
            record.MarkUpdated();
            await _context.SaveChangesAsync();
        }

        public async Task<string> GenerateRecordNumberAsync()
        {
            var next = await _context.ProbationRecords.CountAsync() + 1;
            return $"PRO-{DateTime.Now.Year}-{next:D4}";
        }

        private async Task<ProbationRecord> RequireOpenRecordAsync(int recordId)
        {
            var record = await _context.ProbationRecords
                .Include(p => p.ProgressNotes)
                .FirstOrDefaultAsync(p => p.Id == recordId)
                ?? throw new InvalidOperationException("Probation record was not found.");
            if (!record.IsOpen)
                throw new InvalidOperationException("This probation record is already completed.");
            return record;
        }

        private static ProbationProgressNote CreateSystemNote(
            ProbationRecord record, string status, string notes)
        {
            return new ProbationProgressNote
            {
                ProbationRecord = record,
                NoteDate = DateTime.Now,
                ProgressStatus = status,
                Notes = notes,
                RecordedBy = SessionManager.CurrentUsername
            };
        }
    }
}
