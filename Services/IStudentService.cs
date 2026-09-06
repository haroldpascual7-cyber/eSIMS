using SmartSchoolIncidentManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllStudentsAsync();
        Task<List<Student>> SearchStudentsAsync(string? searchTerm = null, string? gradeLevel = null, string? section = null);
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student?> GetStudentByNumberAsync(string studentNumber);
        Task<bool> AddStudentAsync(Student student);
        Task<bool> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        Task<(int successCount, int failureCount, List<string> errors)> ImportStudentsBatchAsync(List<Student> students);
    }
}
