using System.Security.Cryptography;

namespace MemCheck.Basics
{
    public static class CryptoServices
    {
        public static byte[] GetSHA1(byte[] data)
        {
            using (var sha1 = SHA1.Create())
                return sha1.ComputeHash(data);
        }
    }
}
