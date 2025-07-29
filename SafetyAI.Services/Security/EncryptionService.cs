using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SafetyAI.Services.Security
{
    public static class EncryptionService
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("SafetyAI_Salt_2024");

        /// <summary>
        /// Encrypts data using AES-256 encryption
        /// </summary>
        public static string EncryptData(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Derive key from password
                    using (var keyDerivation = new Rfc2898DeriveBytes(password, Salt, 10000))
                    {
                        aes.Key = keyDerivation.GetBytes(32); // 256 bits
                        aes.IV = keyDerivation.GetBytes(16);  // 128 bits
                    }

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                        csEncrypt.FlushFinalBlock();
                        
                        // Prepend IV to encrypted data
                        var iv = aes.IV;
                        var encryptedData = msEncrypt.ToArray();
                        var result = new byte[iv.Length + encryptedData.Length];
                        Array.Copy(iv, 0, result, 0, iv.Length);
                        Array.Copy(encryptedData, 0, result, iv.Length, encryptedData.Length);
                        
                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts data using AES-256 decryption
        /// </summary>
        public static string DecryptData(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Extract IV from the beginning of the cipher text
                    var iv = new byte[16];
                    var cipher = new byte[fullCipher.Length - 16];
                    Array.Copy(fullCipher, 0, iv, 0, 16);
                    Array.Copy(fullCipher, 16, cipher, 0, cipher.Length);

                    aes.IV = iv;

                    // Derive key from password
                    using (var keyDerivation = new Rfc2898DeriveBytes(password, Salt, 10000))
                    {
                        aes.Key = keyDerivation.GetBytes(32); // 256 bits
                    }

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Hashes a password using PBKDF2 with salt
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty");

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Salt, 10000))
            {
                var hash = pbkdf2.GetBytes(32); // 256 bits
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                var computedHash = HashPassword(password);
                return computedHash == hash;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a secure random token
        /// </summary>
        public static string GenerateSecureToken(int length = 32)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[length];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        /// <summary>
        /// Anonymizes PII data for AI processing
        /// </summary>
        public static string AnonymizePII(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Replace common PII patterns with placeholders
            var anonymized = text;

            // Email addresses
            anonymized = System.Text.RegularExpressions.Regex.Replace(
                anonymized, 
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
                "[EMAIL]");

            // Phone numbers (various formats)
            anonymized = System.Text.RegularExpressions.Regex.Replace(
                anonymized, 
                @"(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})", 
                "[PHONE]");

            // Social Security Numbers
            anonymized = System.Text.RegularExpressions.Regex.Replace(
                anonymized, 
                @"\b\d{3}-?\d{2}-?\d{4}\b", 
                "[SSN]");

            // Names (simple pattern - would need more sophisticated NLP in production)
            anonymized = System.Text.RegularExpressions.Regex.Replace(
                anonymized, 
                @"\b[A-Z][a-z]+ [A-Z][a-z]+\b", 
                "[NAME]");

            return anonymized;
        }
    }
}