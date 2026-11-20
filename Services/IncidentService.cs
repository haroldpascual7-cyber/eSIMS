// Services/IncidentService.cs
// Updated to encrypt Incident.Description using AES‑GCM and store it in DescriptionEncrypted.
// Plaintext Description is kept only in memory for rule‑engine processing and then cleared.

using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using SmartSchoolIncidentManagementSystem.Utilities;

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

            var term = searchTerm.ToLower();
            // NOTE: Description is encrypted, so we cannot search it directly.
            return await _context.Incidents
                .Include(i => i.Student)
                .Include(i => i.Reporter)
                .Where(i =>
                    i.IncidentNumber.ToLower().Contains(term) ||
                    i.Title.ToLower().Contains(term) ||
                    i.Category.ToLower().Contains(term) ||
                    i.Location.ToLower().Contains(term) ||
                    i.PrimaryInvolvedStudent.ToLower().Contains(term) ||
                    i.ReportingStaffTeacher.ToLower().Contains(term) ||
                    i.Id.ToString().Contains(term) ||
                    (i.Student != null && (i.Student.FirstName.ToLower().Contains(term) || i.Student.LastName.ToLower().Contains(term))) ||
                    (i.Reporter != null && (i.Reporter.FirstName.ToLower().Contains(term) || i.Reporter.LastName.ToLower().Contains(term)))
                )
                .OrderByDescending(i => i.IncidentDate)
                .ToListAsync();
        }

        public async Task AddIncidentAsync(Incident incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));

            // Audit fields
            incident.CreatedAt = DateTime.UtcNow;
            incident.UpdatedAt = DateTime.UtcNow;
            incident.IsSynced = false;

            // Encrypt description if provided
            if (!string.IsNullOrWhiteSpace(incident.Description))
            {
                incident.DescriptionEncrypted = AesGcmEncryption.Encrypt(incident.Description);
                // Keep plaintext only in memory for rule engine processing
                incident.Description = null;
            }

            await _context.Incidents.AddAsync(incident);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateIncidentAsync(Incident incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));

            incident.UpdatedAt = DateTime.UtcNow;
            incident.IsSynced = false;

            // Encrypt description if provided (e.g., when editing)
            if (!string.IsNullOrWhiteSpace(incident.Description))
            {
                incident.DescriptionEncrypted = AesGcmEncryption.Encrypt(incident.Description);
                incident.Description = null;
            }

            _context.Incidents.Update(incident);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteIncidentAsync(Incident incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));

            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync();
        }

        // Retained ComputeHash for any legacy uses (not used in the new flow)
        private static string ComputeHash(string plain)
        {
            // Generate a random 16‑byte salt
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);

            // Compute SHA‑256 of (salt + plain text)
            using var sha = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(salt + plain);
            var hashBytes = sha.ComputeHash(combined);
            var hash = Convert.ToBase64String(hashBytes);

            // Store as "salt$hash"
            return $"{salt}${hash}";
        }

        public string? DecryptDescription(Incident incident, User user)
        {
            // TODO: Replace with real permission check (e.g., SessionManager.HasPermission)
            if (user == null) throw new ArgumentNullException(nameof(user));
            // For demonstration, assume any authenticated user can view.
            if (string.IsNullOrEmpty(incident.DescriptionEncrypted)) return null;
            return AesGcmEncryption.Decrypt(incident.DescriptionEncrypted);
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
