using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IConsequenceService
    {
        Task<IEnumerable<ConsequenceLog>> GetAllConsequenceLogsAsync();
        Task<IEnumerable<ConsequenceLog>> SearchAndFilterLogsAsync(string? query, string? categoryFilter, string? statusFilter);
        Task AddConsequenceLogAsync(ConsequenceLog log);
        Task UpdateConsequenceStatusAsync(int id, string newStatus);
        Task<string> GenerateLogIdAsync();
    }
}