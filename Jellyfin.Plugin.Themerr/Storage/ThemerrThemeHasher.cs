using System;
using System.IO;
using System.Security.Cryptography;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Computes theme file hashes used to identify Themerr-managed files.
    /// </summary>
    public static class ThemerrThemeHasher
    {
        /// <summary>
        /// Gets the current theme hash algorithm.
        /// </summary>
        public const string CurrentAlgorithm = ThemerrThemeHashAlgorithm.Sha256;

        /// <summary>
        /// Computes the SHA-256 hash of a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The SHA-256 hash of the file.</returns>
        public static string ComputeHash(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = SHA256.HashData(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
        }
    }
}
