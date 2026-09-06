using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IRestorativeService
    {
        Task<IEnumerable<RestorativePlan>> GetAllInterventionPlansAsync();
        Task<IEnumerable<RestorativePlan>> SearchAndFilterPlansAsync(string? query, string? planType, string? statusFilter);
        Task AddInterventionPlanAsync(RestorativePlan plan);
        Task LogProgressAsync(int planId, int addedUnits, string notes, string staff, bool markCompleted);
        Task UpdatePlanStatusAsync(int planId, string status, bool isCleared);
        Task<string> GeneratePlanIdAsync();
    }
}