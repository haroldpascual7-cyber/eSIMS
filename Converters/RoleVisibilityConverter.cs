using SmartSchoolIncidentManagementSystem.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartSchoolIncidentManagementSystem.Converters
{
    /// <summary>
    /// XAML ValueConverter that evaluates SessionManager.Role against a comma-separated list
    /// of permitted roles passed in ConverterParameter.
    /// Returns Visibility.Visible if allowed; otherwise Visibility.Collapsed.
    /// </summary>
    public class RoleVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(parameterString))
                return Visibility.Visible;

            if (SessionManager.HasModuleAccess(parameterString, parameterString))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
