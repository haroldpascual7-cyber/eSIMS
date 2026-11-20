// Utilities/AesGcmEncryption.cs
// Provides AES‑GCM encryption for protecting incident descriptions at rest.
// The 256‑bit key is loaded from the environment variable "ESIMS_ENC_KEY" (Base64‑encoded).
// Format stored in the DB: version|nonce|tag|ciphertext (all Base64 parts, pipe‑separated).

using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartSchoolIncidentManagementSystem.Utilities
{
    public static class AesGcmEncryption
    {
        private const int NonceSize = 12; // 96‑bit nonce recommended for AES‑GCM
        private const int TagSize = 16;   // 128‑bit authentication tag
        private const string Version = "1";

        private static readonly byte[] Key = LoadKey();

        private static byte[] LoadKey()
        {
            var base64 = Environment.GetEnvironmentVariable("ESIMS_ENC_KEY");
            if (string.IsNullOrWhiteSpace(base64))
            {
                throw new InvalidOperationException("Encryption key not found. Set environment variable ESIMS_ENC_KEY with a Base64‑encoded 32‑byte key.");
            }
            var key = Convert.FromBase64String(base64);
            if (key.Length != 32)
            {
                throw new InvalidOperationException("Encryption key must be 256‑bit (32 bytes).");
            }
            return key;
        }

        /// <summary>
        /// Encrypts plain text and returns a string in the format "version|nonce|tag|ciphertext".
        /// All parts are Base64‑encoded.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(Key);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            var parts = new[]
            {
                Version,
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(ciphertext)
            };
            return string.Join("|", parts);
        }

        /// <summary>
        /// Decrypts a string produced by <see cref="Encrypt"/>.
        /// Returns the original plain text.
        /// </summary>
        public static string Decrypt(string encrypted)
        {
            if (encrypted == null) throw new ArgumentNullException(nameof(encrypted));
            var parts = encrypted.Split('|');
            if (parts.Length != 4) throw new FormatException("Invalid encrypted format.");
            // var version = parts[0]; // currently unused but kept for future upgrades
            var nonce = Convert.FromBase64String(parts[1]);
            var tag = Convert.FromBase64String(parts[2]);
            var ciphertext = Convert.FromBase64String(parts[3]);

            var plaintextBytes = new byte[ciphertext.Length];
            using var aes = new AesGcm(Key);
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}
