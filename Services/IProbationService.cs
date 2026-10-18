using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IProbationService
    {
        Task<IEnumerable<ProbationRecord>> GetAllAsync();
        Task AddAsync(ProbationRecord record);
        Task AddProgressNoteAsync(int recordId, string status, string notes);
        Task ExtendAsync(int recordId, DateTime newEndDate, string reason);
        Task CompleteAsync(int recordId, string outcomeSummary);
        Task<string> GenerateRecordNumberAsync();
    }
}
