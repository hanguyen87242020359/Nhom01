using System;
using System.Security.Cryptography;
using System.Text;

namespace ShopBanHoaLyly.Services
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Hash the plain text password using SHA256 then encode the hash to Base64 string.
        /// </summary>
        /// <param name="plainPassword">The plain text password.</param>
        /// <returns>Base64 encoded SHA256 hash.</returns>
        public static string Hash(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                return string.Empty;
            }

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(plainPassword);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verify if the plain text password matches the hashed password.
        /// </summary>
        /// <param name="plainPassword">The input plain text password.</param>
        /// <param name="hashedPassword">The stored Base64 encoded hash.</param>
        /// <returns>true if match; otherwise false.</returns>
        public static bool Verify(string plainPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            var hashedInput = Hash(plainPassword);
            return hashedPassword == hashedInput;
        }
    }
} 