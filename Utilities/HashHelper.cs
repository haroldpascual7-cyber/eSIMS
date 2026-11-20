using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartSchoolIncidentManagementSystem.Utilities
{
    public static class HashHelper
    {
        /// <summary>
        /// Computes a salted SHA‑256 hash of the given plain text.
        /// Returns a string formatted as "{salt}${hash}" with both parts Base64‑encoded.
        /// </summary>
        public static string ComputeHash(string plain)
        {
            if (string.IsNullOrEmpty(plain))
                return plain;

            // Generate a random 16‑byte salt
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);

            // SHA‑256(salt + plain)
            using var sha = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(salt + plain);
            var hashBytes = sha.ComputeHash(combined);
            var hash = Convert.ToBase64String(hashBytes);

            return $"{salt}${hash}";
        }
    }
}
