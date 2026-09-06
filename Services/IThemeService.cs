namespace SmartSchoolIncidentManagementSystem.Services
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        void SetTheme(AppTheme theme);
        void ToggleTheme();
    }
}
