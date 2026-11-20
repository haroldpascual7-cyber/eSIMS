using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using SmartSchoolIncidentManagementSystem.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class LoginViewModel
    {
        private const string SqliteOption = "SQLite (Local File)";
        private const string MySqlOption = "MySQL (Server)";

        private string _selectedDatabaseOption = SqliteOption;

        public ObservableCollection<string> DatabaseOptions { get; } = new()
        {
            SqliteOption,
            MySqlOption
        };

        public string SelectedDatabaseOption
        {
            get => _selectedDatabaseOption;
            set
            {
                if (_selectedDatabaseOption == value) return;

                var targetProvider = value == MySqlOption
                    ? DatabaseProviderType.MySql
                    : DatabaseProviderType.Sqlite;

                if (targetProvider == DatabaseProvider.ActiveProvider)
                {
                    SetProperty(ref _selectedDatabaseOption, value);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Switching database provider to {targetProvider} requires an application restart.\n\nRestart now?",
                    "Switch Database Provider",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    _selectedDatabaseOption = value;
                    OnPropertyChanged();
                    DatabaseProvider.SetProvider(targetProvider);
                    RestartApplication();
                }
                else
                {
                    // Revert without restart
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConfigureDatabaseCommand { get; private set; } = null!;

        private void InitializeDatabaseSettings()
        {
            _selectedDatabaseOption = DatabaseProvider.IsMySql ? MySqlOption : SqliteOption;
            ConfigureDatabaseCommand = new RelayCommand(OpenDatabaseConfig);
        }

        private void OpenDatabaseConfig()
        {
            var configWindow = new MySqlConfigWindow
            {
                Owner = Application.Current?.MainWindow
            };

            bool? result = configWindow.ShowDialog();
            if (result == true && DatabaseProvider.IsMySql)
            {
                var restartConfirm = MessageBox.Show(
                    "MySQL database configuration has been saved.\n\nRestart the application now to connect with the updated configuration?",
                    "Configuration Saved",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (restartConfirm == MessageBoxResult.Yes)
                {
                    RestartApplication();
                }
            }
        }

        public static void RestartApplication()
        {
            try
            {
                string? exe = Process.GetCurrentProcess().MainModule?.FileName
                              ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

                if (!string.IsNullOrEmpty(exe))
                {
                    Process.Start(new ProcessStartInfo(exe)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = AppContext.BaseDirectory
                    });
                }
            }
            catch { }
            finally
            {
                Application.Current?.Shutdown();
            }
        }
    }
}
