using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly AppDbContext _context;

        public IncidentService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Incident>> GetAllIncidentsAsync()
        {
            return await _context.Incidents
                .Include(i => i.Student)
                .Include(i => i.Reporter)
                .OrderByDescending(i => i.IncidentDate)
                .ToListAsync();
        }

        public async Task<Incident?> GetIncidentByIdAsync(int id)
        {
            return await _context.Incidents
                .Include(i => i.Student)
                .Include(i => i.Reporter)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Incident>> GetIncidentsByStatusAsync(IncidentStatus status)
        {
            return await _context.Incidents
                .Include(i => i.Student)
                .Include(i => i.Reporter)
                .Where(i => i.Status == status)
                .OrderByDescending(i => i.IncidentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Incident>> GetIncidentsBySeverityAsync(IncidentSeverity severity)
        {
            return await _context.Incidents
                .Include(i => i.Student)
                .Include(i => i.Reporter)
                .Where(i => i.Severity == severity)
                .OrderByDescending(i => i.IncidentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Incident>> SearchIncidentsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllIncidentsAsync();
            }

            searchTerm = searchTerm.ToLower();

            return await _context.Incidents
                .Include(i => i.Student)
                .Include(i => i.Reporter)
                .Where(i => i.IncidentNumber.ToLower().Contains(searchTerm) ||
                            i.Title.ToLower().Contains(searchTerm) ||
                            i.Category.ToLower().Contains(searchTerm) ||
                            i.Description.ToLower().Contains(searchTerm) ||
                            i.Location.ToLower().Contains(searchTerm) ||
                            i.PrimaryInvolvedStudent.ToLower().Contains(searchTerm) ||
                            i.ReportingStaffTeacher.ToLower().Contains(searchTerm) ||
                            (i.Student != null && (i.Student.FirstName.ToLower().Contains(searchTerm) || i.Student.LastName.ToLower().Contains(searchTerm))) ||
                            (i.Reporter != null && (i.Reporter.FirstName.ToLower().Contains(searchTerm) || i.Reporter.LastName.ToLower().Contains(searchTerm))))
                .OrderByDescending(i => i.IncidentDate)
                .ToListAsync();
        }

        public async Task AddIncidentAsync(Incident incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));
            
            incident.CreatedAt = DateTime.UtcNow;
            incident.UpdatedAt = DateTime.UtcNow;
            incident.IsSynced = false;

            await _context.Incidents.AddAsync(incident);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateIncidentAsync(Incident incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));

            incident.UpdatedAt = DateTime.UtcNow;
            incident.IsSynced = false;

            _context.Incidents.Update(incident);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteIncidentAsync(Incident incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));

            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync()
        {
            return await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetAllStaffAsync()
        {
            return await _context.StaffMembers
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
    }
}
