using System.Security.Cryptography;

namespace MemCheck.Basics
{
    public static class CryptoServices
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "This is not used in any security sensible case")]
        public static byte[] GetSHA1(byte[] data)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(data);
        }
    }
}
