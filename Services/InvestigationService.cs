using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class InvestigationService : IInvestigationService
    {
        private readonly AppDbContext _context;

        public InvestigationService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Investigation>> GetAllInvestigationsAsync()
        {
            return await _context.Investigations
                .Include(i => i.Incident)
                    .ThenInclude(inc => inc!.Student)
                .Include(i => i.Incident)
                    .ThenInclude(inc => inc!.Reporter)
                .Include(i => i.Investigator)
                .OrderByDescending(i => i.StartDate)
                .ToListAsync();
        }

        public async Task<Investigation?> GetInvestigationByIncidentIdAsync(int incidentId)
        {
            return await _context.Investigations
                .Include(i => i.Incident)
                    .ThenInclude(inc => inc!.Student)
                .Include(i => i.Incident)
                    .ThenInclude(inc => inc!.Reporter)
                .Include(i => i.Investigator)
                .FirstOrDefaultAsync(i => i.IncidentId == incidentId);
        }

        public async Task<Investigation> GetOrCreateInvestigationAsync(int incidentId)
        {
            var existing = await GetInvestigationByIncidentIdAsync(incidentId);
            if (existing != null) return existing;

            var incident = await _context.Incidents.FindAsync(incidentId);
            var newInv = new Investigation
            {
                IncidentId = incidentId,
                Incident = incident,
                StartDate = DateTime.Now,
                TargetCompletionDate = DateTime.Now.AddDays(7),
                Findings = "Pending"
            };

            await _context.Investigations.AddAsync(newInv);
            if (incident != null && incident.Status == IncidentStatus.Reported)
            {
                incident.Status = IncidentStatus.UnderInvestigation;
                incident.MarkUpdated();
            }

            await _context.SaveChangesAsync();
            return newInv;
        }

        public async Task SaveInvestigationAsync(Investigation investigation)
        {
            if (investigation == null) throw new ArgumentNullException(nameof(investigation));

            investigation.MarkUpdated();
            if (investigation.Id == 0)
            {
                await _context.Investigations.AddAsync(investigation);
            }
            else
            {
                _context.Investigations.Update(investigation);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DisciplinaryAction>> GetAllDisciplinaryActionsAsync()
        {
            return await _context.DisciplinaryActions
                .Include(d => d.Incident)
                .Include(d => d.Student)
                .Include(d => d.AssignedStaff)
                .OrderByDescending(d => d.ScheduledDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<DisciplinaryAction>> GetDisciplinaryActionsByIncidentIdAsync(int incidentId)
        {
            return await _context.DisciplinaryActions
                .Include(d => d.Student)
                .Include(d => d.AssignedStaff)
                .Where(d => d.IncidentId == incidentId)
                .OrderByDescending(d => d.ScheduledDate)
                .ToListAsync();
        }

        public async Task AddDisciplinaryActionAsync(DisciplinaryAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action.CreatedAt = DateTime.UtcNow;
            action.UpdatedAt = DateTime.UtcNow;
            action.IsSynced = false;

            await _context.DisciplinaryActions.AddAsync(action);

            var incident = await _context.Incidents.FindAsync(action.IncidentId);
            if (incident != null && (incident.Status == IncidentStatus.Reported || incident.Status == IncidentStatus.UnderInvestigation))
            {
                incident.Status = IncidentStatus.ActionTaken;
                incident.MarkUpdated();
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateDisciplinaryActionAsync(DisciplinaryAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action.MarkUpdated();
            _context.DisciplinaryActions.Update(action);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDisciplinaryActionAsync(DisciplinaryAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            _context.DisciplinaryActions.Remove(action);
            await _context.SaveChangesAsync();
        }

        public async Task CompleteDisciplinaryActionAndResolveIncidentAsync(int actionId, string? notes, bool markIncidentResolved)
        {
            var action = await _context.DisciplinaryActions.FindAsync(actionId);
            if (action != null)
            {
                action.Status = "Completed";
                action.CompletedDate = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    action.FollowUpNotes = string.IsNullOrWhiteSpace(action.FollowUpNotes)
                        ? notes.Trim()
                        : $"{action.FollowUpNotes}\n[Guidance Note]: {notes.Trim()}";
                }
                action.MarkUpdated();
                _context.DisciplinaryActions.Update(action);

                if (markIncidentResolved)
                {
                    var incident = await _context.Incidents.FindAsync(action.IncidentId);
                    if (incident != null)
                    {
                        incident.Status = IncidentStatus.Resolved;
                        incident.MarkUpdated();
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateIncidentStatusAsync(int incidentId, IncidentStatus newStatus)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident != null)
            {
                incident.Status = newStatus;
                incident.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }
    }
}