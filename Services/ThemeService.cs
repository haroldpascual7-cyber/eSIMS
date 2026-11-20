using System;
using System.Linq;
using System.Windows;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class ThemeService : IThemeService
    {
        public event Action<AppTheme>? ThemeChanged;
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public ThemeService()
        {
            SetTheme(AppTheme.Light);
        }

        public void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            var themeUri = theme == AppTheme.Light
                ? new Uri("Styles/LightTheme.xaml", UriKind.Relative)
                : new Uri("Styles/DarkTheme.xaml", UriKind.Relative);

            var newThemeDict = new ResourceDictionary { Source = themeUri };
            var appResources = Application.Current.Resources;

            // In-place resource key assignment prevents WPF resource lookup null invalidation crashes
            foreach (var key in newThemeDict.Keys)
            {
                appResources[key] = newThemeDict[key];
            }

            // Update MergedDictionaries index cleanly
            var merged = appResources.MergedDictionaries;
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.OriginalString ?? string.Empty;
                if (src.Contains("LightTheme.xaml") || src.Contains("DarkTheme.xaml"))
                {
                    merged[i] = newThemeDict;
                    ThemeChanged?.Invoke(CurrentTheme);
                    return;
                }
            }

            merged.Insert(0, newThemeDict);
            ThemeChanged?.Invoke(CurrentTheme);
        }

        public void ToggleTheme()
        {
            SetTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
        }
    }
}
