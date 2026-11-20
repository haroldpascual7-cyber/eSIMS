using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Helpers;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public User? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser != null || SessionManager.IsAuthenticated;

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Please enter both username and password.", null);
            }

            var trimmedUsername = username.Trim();
            string throttleKey = $"{trimmedUsername}|{Environment.MachineName}";

            // 1. Throttle check
            if (LoginThrottle.IsLockedOut(throttleKey, out var remaining))
            {
                int minutes = (int)Math.Ceiling(remaining.TotalMinutes);
                if (minutes < 1) minutes = 1;
                return (false, $"Too many failed attempts. Try again in {minutes} minute{(minutes == 1 ? "" : "s")}.", null);
            }

            // 2. Try SystemUsers table first
            try
            {
                DatabaseInitializer.InitializeSystemUsers();
                using var conn = DatabaseProvider.CreateConnection();
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT UserId, Username, FullName, Email, Role, Status, PasswordHash
                    FROM SystemUsers
                    WHERE LOWER(Username) = LOWER(@Username) OR LOWER(Email) = LOWER(@Username);";
                cmd.AddParam("@Username", trimmedUsername);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int userId = reader.GetInt32(0);
                    string userUname = reader.GetString(1);
                    string fullName = reader.GetString(2);
                    string email = reader.GetString(3);
                    string role = reader.GetString(4);
                    string status = reader.GetString(5);
                    string storedHash = reader.GetString(6);

                    if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "This account has been suspended. Please contact your administrator.", null);
                    }

                    bool passwordValid = PasswordHasher.Verify(password, storedHash, out bool needsRehash);

                    if (passwordValid)
                    {
                        await reader.CloseAsync();

                        if (needsRehash)
                        {
                            try
                            {
                                string newHash = PasswordHasher.Hash(password);
                                using var updateCmd = conn.CreateCommand();
                                updateCmd.CommandText = "UPDATE SystemUsers SET PasswordHash = @NewHash WHERE UserId = @UserId;";
                                updateCmd.AddParam("@NewHash", newHash);
                                updateCmd.AddParam("@UserId", userId);
                                await updateCmd.ExecuteNonQueryAsync();

                                var efU = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId || u.Username.ToLower() == trimmedUsername.ToLower());
                                if (efU != null)
                                {
                                    efU.PasswordHash = newHash;
                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch { /* Ignore rehash persistence errors */ }
                        }

                        LoginThrottle.Reset(throttleKey);
                        SessionManager.SetSession(trimmedUsername, fullName, role);

                        var user = new User
                        {
                            Id = userId,
                            Username = userUname,
                            Email = email,
                            Role = role,
                            IsActive = true
                        };
                        CurrentUser = user;

                        await AuditLogger.LogAsync(trimmedUsername, "LOGIN", "Authentication", $"User {trimmedUsername} logged in successfully as {role}.");
                        return (true, "Login successful.", user);
                    }
                    else
                    {
                        LoginThrottle.RecordFailure(throttleKey);
                        await AuditLogger.LogAsync(trimmedUsername, "FAILED_LOGIN", "Authentication", $"Failed login attempt for user {trimmedUsername}.");
                        return (false, "Invalid username or password.", null);
                    }
                }
            }
            catch { /* Fallback to EF Core Users table */ }

            // 3. Fallback to EF Core Users table
            var efUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == trimmedUsername.ToLower() || u.Email.ToLower() == trimmedUsername.ToLower());

            if (efUser != null)
            {
                if (!efUser.IsActive)
                {
                    return (false, "This account has been deactivated. Please contact your administrator.", null);
                }

                bool efValid = PasswordHasher.Verify(password, efUser.PasswordHash, out bool efNeedsRehash);
                if (efValid)
                {
                    if (efNeedsRehash)
                    {
                        try
                        {
                            efUser.PasswordHash = PasswordHasher.Hash(password);
                            await _context.SaveChangesAsync();
                        }
                        catch { }
                    }

                    LoginThrottle.Reset(throttleKey);
                    CurrentUser = efUser;
                    SessionManager.SetSession(efUser.Username, efUser.Username, efUser.Role);
                    await AuditLogger.LogAsync(efUser.Username, "LOGIN", "Authentication", $"User {efUser.Username} logged in successfully.");
                    return (true, "Login successful.", efUser);
                }
            }

            LoginThrottle.RecordFailure(throttleKey);
            await AuditLogger.LogAsync(trimmedUsername, "FAILED_LOGIN", "Authentication", $"Failed login attempt for user {trimmedUsername}.");
            return (false, "Invalid username or password.", null);
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string fullName, string email, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
            {
                return (false, "Please complete all required fields.");
            }

            string trimmedUsername = username.Trim();
            string trimmedFullName = fullName.Trim();
            string trimmedEmail = email.Trim();
            string trimmedRole = role.Trim();

            if (password.Length < 6)
            {
                return (false, "Password must be at least 6 characters long.");
            }

            try
            {
                // 1. Check if username exists in SystemUsers
                DatabaseInitializer.InitializeSystemUsers();
                using var conn = DatabaseProvider.CreateConnection();
                await conn.OpenAsync();

                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM SystemUsers WHERE LOWER(Username) = LOWER(@Username);";
                checkCmd.AddParam("@Username", trimmedUsername);
                var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    return (false, $"Username '{trimmedUsername}' is already taken. Please choose another.");
                }

                // 2. Also check EF Core Users table
                bool efExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == trimmedUsername.ToLower());
                if (efExists)
                {
                    return (false, $"Username '{trimmedUsername}' is already taken. Please choose another.");
                }

                // 3. Hash password
                string hashedPassword = PasswordHasher.Hash(password);

                // 4. Insert into SystemUsers table
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO SystemUsers (Username, FullName, Email, PasswordHash, Role, Status, CreatedAt)
                    VALUES (@Username, @FullName, @Email, @PasswordHash, @Role, 'Active', CURRENT_TIMESTAMP);";
                insertCmd.AddParam("@Username", trimmedUsername);
                insertCmd.AddParam("@FullName", trimmedFullName);
                insertCmd.AddParam("@Email", trimmedEmail);
                insertCmd.AddParam("@PasswordHash", hashedPassword);
                insertCmd.AddParam("@Role", trimmedRole);
                await insertCmd.ExecuteNonQueryAsync();

                // 5. Also add to EF Core Users table
                var user = new User
                {
                    Username = trimmedUsername,
                    PasswordHash = hashedPassword,
                    Email = trimmedEmail,
                    Role = trimmedRole,
                    IsActive = true
                };
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // 6. Audit log
                await AuditLogger.LogAsync(trimmedUsername, "REGISTER", "Authentication", $"New user '{trimmedUsername}' ({trimmedFullName}) registered with role '{trimmedRole}'.");

                return (true, "Account created successfully! You can now sign in.");
            }
            catch (Exception ex)
            {
                return (false, $"Registration failed: {ex.Message}");
            }
        }

        public void Logout()
        {
            if (CurrentUser != null)
            {
                _ = AuditLogger.LogAsync(CurrentUser.Username, "LOGOUT", "Authentication", $"User {CurrentUser.Username} logged out.");
            }
            CurrentUser = null;
            SessionManager.Clear();
        }
    }
}
