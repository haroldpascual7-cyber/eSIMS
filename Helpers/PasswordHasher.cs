using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartSchoolIncidentManagementSystem.Helpers
{
    /// <summary>
    /// Password hashing using PBKDF2-HMAC-SHA256 with a 256-bit derived key.
    /// Stored format: PBKDF2-SHA256$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 hash&gt;
    /// Legacy SHA-256 and plaintext hashes are still verifiable for backward
    /// compatibility and are transparently upgraded after a successful login.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "PBKDF2-SHA256";
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256-bit
        private const int Iterations = 120_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string? password, string? storedHash, out bool needsRehash)
        {
            needsRehash = false;

            if (password == null || string.IsNullOrEmpty(storedHash))
                return false;

            if (storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
            {
                return VerifyPbkdf2(password, storedHash, out needsRehash);
            }

            if (IsLegacySha256(storedHash))
            {
                needsRehash = true;
                string computed = SecurityHelper.ComputeSha256Hash(password);
                return FixedTimeEquals(computed, storedHash);
            }

            // Legacy plaintext seed data: verify once, then force a rehash.
            needsRehash = true;
            return FixedTimeEquals(storedHash, password);
        }

        private static bool VerifyPbkdf2(string password, string storedHash, out bool needsRehash)
        {
            needsRehash = false;

            string[] parts = storedHash.Split('$');
            if (parts.Length != 4)
                return false;

            if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

                bool valid = CryptographicOperations.FixedTimeEquals(actual, expected);

                if (valid && iterations < Iterations)
                    needsRehash = true;

                return valid;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLegacySha256(string value)
        {
            if (value.Length != 64) return false;

            foreach (char c in value)
            {
                if (!Uri.IsHexDigit(c)) return false;
            }

            return true;
        }

        private static bool FixedTimeEquals(string? a, string? b)
        {
            if (a == null || b == null) return false;

            byte[] left = Encoding.UTF8.GetBytes(a);
            byte[] right = Encoding.UTF8.GetBytes(b);

            return left.Length == right.Length &&
                   CryptographicOperations.FixedTimeEquals(left, right);
        }
    }
}
