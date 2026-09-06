using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IAnecdotalService
    {
        Task<List<AnecdotalRecordItem>> GetAllAsync(string? searchTerm = null, string? typeFilter = null);
        Task<AnecdotalRecordItem> AddAsync(AnecdotalRecordItem item);
        Task<bool> DeleteAsync(int id);
    }
}
