using S3Encryption.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Utils
{
    static class EncryptionUtilities
    {
        const string DEFAULT_SYMMETRIC_ALGORITHM = "AES";
        const int DEFAULT_SYMMETRIC_KEY_LENGTH = 256;

        public static byte[] GenerateEncodedSymmetricKey()
        {
            return ASCIIEncoding.UTF8.GetBytes(Convert.ToBase64String(GenerateSymmetricKey()));
        }

        public static byte[] GenerateSymmetricKey()
        {
            var symKey = GenerateSecretKey();
            return symKey.GetEncoded();
        }

        public static RSA GenerateSecretKey()
        {
            return RSA.Create();
        }

    }
}
