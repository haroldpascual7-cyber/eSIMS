using Microsoft.Data.Sqlite;
using MySqlConnector;
using SmartSchoolIncidentManagementSystem.Helpers;
using System;
using System.Data.Common;
using System.IO;
using System.Text.Json;

namespace SmartSchoolIncidentManagementSystem.Data
{
    public enum DatabaseProviderType
    {
        Sqlite,
        MySql
    }

    public class DatabaseConfig
    {
        public string Provider { get; set; } = "Sqlite";
        public string MySqlServer { get; set; } = "localhost";
        public int MySqlPort { get; set; } = 3306;
        public string MySqlDatabase { get; set; } = "smartschool_incidents";
        public string MySqlUserId { get; set; } = "root";
        public string MySqlPasswordProtected { get; set; } = string.Empty;
    }

    public static class DatabaseProvider
    {
        private static readonly object _lock = new();

        public static DatabaseProviderType ActiveProvider { get; private set; } = DatabaseProviderType.Sqlite;
        public static bool IsMySql => ActiveProvider == DatabaseProviderType.MySql;
        public static DatabaseConfig Config { get; private set; } = new();

        public static string ConfigFolder { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool");

        public static string ConfigPath { get; } =
            Path.Combine(ConfigFolder, "database.config.json");

        public static string InsertOrReplaceKeyword => IsMySql ? "REPLACE" : "INSERT OR REPLACE";
        public static string ProviderDisplayName => IsMySql ? "MySQL" : "SQLite";

        public static string MySqlPassword
        {
            get => AesEncryptionHelper.Unprotect(Config.MySqlPasswordProtected);
            set => Config.MySqlPasswordProtected = AesEncryptionHelper.Protect(value);
        }

        public static void Initialize()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(ConfigFolder);

                    if (File.Exists(ConfigPath))
                    {
                        string json = File.ReadAllText(ConfigPath);
                        var loaded = JsonSerializer.Deserialize<DatabaseConfig>(json);
                        if (loaded != null)
                        {
                            Config = loaded;
                        }
                    }
                    else
                    {
                        Config = new DatabaseConfig();
                        SaveConfig();
                    }
                }
                catch
                {
                    Config = new DatabaseConfig();
                }

                if (Enum.TryParse<DatabaseProviderType>(Config.Provider, true, out var parsed))
                {
                    ActiveProvider = parsed;
                }
                else
                {
                    ActiveProvider = DatabaseProviderType.Sqlite;
                    Config.Provider = "Sqlite";
                }
            }
        }

        public static void SetProvider(DatabaseProviderType p)
        {
            lock (_lock)
            {
                ActiveProvider = p;
                Config.Provider = p.ToString();
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(ConfigFolder);
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(Config, options);
                    File.WriteAllText(ConfigPath, json);
                }
                catch
                {
                    // Ignore write failures in restricted environments
                }
            }
        }

        public static string GetSqliteConnectionString()
        {
            string dbPath = Path.Combine(ConfigFolder, "incidents.db");
            return $"Data Source={dbPath}";
        }

        public static string BuildMySqlConnectionString(bool includeDatabase)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = string.IsNullOrWhiteSpace(Config.MySqlServer) ? "localhost" : Config.MySqlServer,
                Port = (uint)Math.Clamp(Config.MySqlPort > 0 ? Config.MySqlPort : 3306, 1, 65535),
                UserID = string.IsNullOrWhiteSpace(Config.MySqlUserId) ? "root" : Config.MySqlUserId,
                Password = MySqlPassword,
                SslMode = MySqlSslMode.Preferred,
                AllowPublicKeyRetrieval = true,
                CharacterSet = "utf8mb4",
                AllowUserVariables = true,
                ConnectionTimeout = 10
            };

            if (includeDatabase && !string.IsNullOrWhiteSpace(Config.MySqlDatabase))
            {
                builder.Database = Config.MySqlDatabase;
            }

            return builder.ConnectionString;
        }

        public static string GetConnectionString()
        {
            return IsMySql ? BuildMySqlConnectionString(true) : GetSqliteConnectionString();
        }

        public static DbConnection CreateConnection()
        {
            if (IsMySql)
            {
                return new MySqlConnection(GetConnectionString());
            }

            return new SqliteConnection(GetSqliteConnectionString());
        }

        public static void ApplyMySqlSettings(string server, int port, string db, string user, string password)
        {
            lock (_lock)
            {
                Config.MySqlServer = server?.Trim() ?? "localhost";
                Config.MySqlPort = port > 0 ? port : 3306;
                Config.MySqlDatabase = db?.Trim() ?? "smartschool_incidents";
                Config.MySqlUserId = user?.Trim() ?? "root";
                MySqlPassword = password ?? string.Empty;
            }
        }

        public static bool EnsureMySqlDatabase(out string error)
        {
            error = string.Empty;
            try
            {
                string adminConnStr = BuildMySqlConnectionString(false);
                using var conn = new MySqlConnection(adminConnStr);
                conn.Open();

                string safeDbName = Config.MySqlDatabase.Replace("`", "``");
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{safeDbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TestConnection(out string error)
        {
            error = string.Empty;
            try
            {
                using var conn = CreateConnection();
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
