using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IInvestigationService
    {
        Task<IEnumerable<Investigation>> GetAllInvestigationsAsync();
        Task<Investigation?> GetInvestigationByIncidentIdAsync(int incidentId);
        Task<Investigation> GetOrCreateInvestigationAsync(int incidentId);
        Task SaveInvestigationAsync(Investigation investigation);
        Task<IEnumerable<DisciplinaryAction>> GetDisciplinaryActionsByIncidentIdAsync(int incidentId);
        Task AddDisciplinaryActionAsync(DisciplinaryAction action);
        Task UpdateDisciplinaryActionAsync(DisciplinaryAction action);
        Task DeleteDisciplinaryActionAsync(DisciplinaryAction action);
        Task UpdateIncidentStatusAsync(int incidentId, IncidentStatus newStatus);
    }
}