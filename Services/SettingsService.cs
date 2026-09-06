using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string SchoolName { get; private set; } = "Smart Academy";
        public string SystemTitle { get; private set; } = "Incident Management System";
        public string Tagline { get; private set; } = "Fostering a safe, accountable, and supportive educational community.";
        public string Motto { get; private set; } = "Safe Campus • Real-time Reporting • Smart Resolution";
        public string LogoIcon { get; private set; } = "🛡️";

        public async Task LoadSettingsAsync()
        {
            try
            {
                SchoolName = await GetSettingAsync("SchoolName", "Smart Academy");
                SystemTitle = await GetSettingAsync("SystemTitle", "Incident Management System");
                Tagline = await GetSettingAsync("Tagline", "Fostering a safe, accountable, and supportive educational community.");
                Motto = await GetSettingAsync("Motto", "Safe Campus • Real-time Reporting • Smart Resolution");
                LogoIcon = await GetSettingAsync("LogoIcon", "🛡️");
            }
            catch
            {
                // Fallback to defaults if DB is initializing
            }
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting != null ? setting.Value : defaultValue;
        }

        public async Task SetSettingAsync(string key, string value, string description = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Description = description
                };
                await _context.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                if (!string.IsNullOrEmpty(description))
                {
                    setting.Description = description;
                }
                setting.MarkUpdated();
            }

            await _context.SaveChangesAsync();
            await LoadSettingsAsync();
        }
    }
}
