namespace SmartSchoolIncidentManagementSystem.Services
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public interface IThemeService
    {
        event System.Action<AppTheme>? ThemeChanged;
        AppTheme CurrentTheme { get; }
        void SetTheme(AppTheme theme);
        void ToggleTheme();
    }
}
