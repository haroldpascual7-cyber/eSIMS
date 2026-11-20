using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmartSchoolIncidentManagementSystem.Helpers
{
    /// <summary>
    /// AES-256-CBC encryption with an HMAC-SHA256 authentication tag (encrypt-then-MAC).
    /// The key material is derived from the machine/user context so that protected
    /// configuration secrets cannot be trivially moved to another workstation.
    /// </summary>
    public static class AesEncryptionHelper
    {
        private const int IvSize = 16;
        private const int MacSize = 32;
        private const string AppSalt = "SmartSchoolIncidentManagementSystem::AES::v1";

        private static byte[] DeriveKey(string purpose)
        {
            string material = string.Join("|",
                AppSalt,
                purpose,
                Environment.MachineName,
                Environment.UserName);

            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(material));
        }

        public static string Protect(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            byte[] key = DeriveKey("encryption");
            byte[] macKey = DeriveKey("authentication");

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            byte[] iv = aes.IV;
            byte[] cipher;

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] input = Encoding.UTF8.GetBytes(plainText);
                cipher = encryptor.TransformFinalBlock(input, 0, input.Length);
            }

            using var hmac = new HMACSHA256(macKey);
            byte[] tag = hmac.ComputeHash(Combine(iv, cipher));

            return Convert.ToBase64String(Combine(Combine(iv, tag), cipher));
        }

        public static string Unprotect(string? protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return string.Empty;

            try
            {
                byte[] payload = Convert.FromBase64String(protectedText);
                if (payload.Length <= IvSize + MacSize) return string.Empty;

                byte[] iv = new byte[IvSize];
                byte[] tag = new byte[MacSize];
                byte[] cipher = new byte[payload.Length - IvSize - MacSize];

                Buffer.BlockCopy(payload, 0, iv, 0, IvSize);
                Buffer.BlockCopy(payload, IvSize, tag, 0, MacSize);
                Buffer.BlockCopy(payload, IvSize + MacSize, cipher, 0, cipher.Length);

                byte[] key = DeriveKey("encryption");
                byte[] macKey = DeriveKey("authentication");

                using var hmac = new HMACSHA256(macKey);
                byte[] expected = hmac.ComputeHash(Combine(iv, cipher));
                if (!CryptographicOperations.FixedTimeEquals(expected, tag))
                    return string.Empty;

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                byte[] clear = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(clear);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }
    }
}
