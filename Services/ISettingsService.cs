using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface ISettingsService
    {
        string SchoolName { get; }
        string SchoolAddress { get; }
        string SystemTitle { get; }
        string SystemAcronym { get; }
        string Tagline { get; }
        string Motto { get; }
        string LogoIcon { get; }
        string LoginLogoPath { get; }
        string DashboardLogoPath { get; }
        object? LoginLogoSource { get; }
        object? DashboardLogoSource { get; }

        string MasterGradeLevels { get; }
        string MasterSections { get; }
        string MasterDepartments { get; }

        Task<System.Collections.Generic.List<string>> GetAllDiscoveredSectionsAsync();
        Task<System.Collections.Generic.List<string>> GetAllDiscoveredGradeLevelsAsync();
        Task<System.Collections.Generic.List<string>> GetAllDiscoveredDepartmentsAsync();

        event System.Action? SettingsChanged;

        Task LoadSettingsAsync();
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task SetSettingAsync(string key, string value, string description = "");
    }
}
