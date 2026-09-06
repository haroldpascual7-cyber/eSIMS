using SmartSchoolIncidentManagementSystem.Models;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password);
        Task<(bool Success, string Message)> RegisterAsync(string username, string fullName, string email, string password, string role);
        void Logout();
    }
}
