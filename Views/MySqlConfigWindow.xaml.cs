using MySqlConnector;
using SmartSchoolIncidentManagementSystem.Data;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class MySqlConfigWindow : Window
    {
        public MySqlConfigWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var config = DatabaseProvider.Config;
            ServerBox.Text = config.MySqlServer;
            PortBox.Text = config.MySqlPort.ToString();
            DatabaseBox.Text = config.MySqlDatabase;
            UsernameBox.Text = config.MySqlUserId;
            PasswordBox.Password = DatabaseProvider.MySqlPassword;
        }

        private void OnTestConnectionClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                ShowStatus("Invalid port number. Please specify a value between 1 and 65535.", false);
                return;
            }

            try
            {
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = string.IsNullOrWhiteSpace(ServerBox.Text) ? "localhost" : ServerBox.Text.Trim(),
                    Port = (uint)port,
                    UserID = string.IsNullOrWhiteSpace(UsernameBox.Text) ? "root" : UsernameBox.Text.Trim(),
                    Password = PasswordBox.Password,
                    SslMode = MySqlSslMode.Preferred,
                    AllowPublicKeyRetrieval = true,
                    CharacterSet = "utf8mb4",
                    AllowUserVariables = true,
                    ConnectionTimeout = 5
                };

                using var conn = new MySqlConnection(builder.ConnectionString);
                conn.Open();

                ShowStatus("Connection successful! MySQL server is reachable.", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Connection failed: {ex.Message}", false);
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                ShowStatus("Invalid port number (must be 1–65535).", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(ServerBox.Text))
            {
                ShowStatus("Server address cannot be empty.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(DatabaseBox.Text))
            {
                ShowStatus("Database name cannot be empty.", false);
                return;
            }

            DatabaseProvider.ApplyMySqlSettings(
                ServerBox.Text.Trim(),
                port,
                DatabaseBox.Text.Trim(),
                UsernameBox.Text.Trim(),
                PasswordBox.Password);

            DatabaseProvider.SaveConfig();
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            StatusBorder.Visibility = Visibility.Visible;
            StatusText.Text = message;

            if (isSuccess)
            {
                StatusBorder.SetResourceReference(Border.BackgroundProperty, "SuccessBgBrush");
                StatusBorder.SetResourceReference(Border.BorderBrushProperty, "SuccessBrush");
                StatusText.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            }
            else
            {
                StatusBorder.SetResourceReference(Border.BackgroundProperty, "DangerBgBrush");
                StatusBorder.SetResourceReference(Border.BorderBrushProperty, "DangerBrush");
                StatusText.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            }
        }
    }
}
