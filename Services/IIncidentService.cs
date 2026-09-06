using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IIncidentService
    {
        Task<IEnumerable<Incident>> GetAllIncidentsAsync();
        Task<Incident?> GetIncidentByIdAsync(int id);
        Task<IEnumerable<Incident>> GetIncidentsByStatusAsync(IncidentStatus status);
        Task<IEnumerable<Incident>> GetIncidentsBySeverityAsync(IncidentSeverity severity);
        Task<IEnumerable<Incident>> SearchIncidentsAsync(string searchTerm);
        Task AddIncidentAsync(Incident incident);
        Task UpdateIncidentAsync(Incident incident);
        Task DeleteIncidentAsync(Incident incident);
        
        // Student & Staff lookups for form dropdowns
        Task<IEnumerable<Student>> GetAllStudentsAsync();
        Task<IEnumerable<Staff>> GetAllStaffAsync();
    }
}
