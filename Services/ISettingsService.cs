using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface ISettingsService
    {
        string SchoolName { get; }
        string SystemTitle { get; }
        string Tagline { get; }
        string Motto { get; }
        string LogoIcon { get; }

        Task LoadSettingsAsync();
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task SetSettingAsync(string key, string value, string description = "");
    }
}
