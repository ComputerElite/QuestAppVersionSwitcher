using System;
using System.IO;
using System.Security.Cryptography;

namespace QuestAppVersionSwitcher
{
    public class Utils
    {
        public static string GetSHA256OfFile(string filePath)
        {
            byte[] hash;
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(fileStream);
                }
            }

            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}