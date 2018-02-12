using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace S3Encryption
{
    static class RSAKeyExtensions
    {
        public static byte [] GetEncoded(this RSA rsa)
        {
            var parameters = rsa.ExportParameters(true);

            var serializedString = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);

            return Encoding.UTF8.GetBytes(serializedString);
        }

        public static void ImportEncodedParameters(this RSA rsa, byte [] data)
        {
            var parameterString = Encoding.UTF8.GetString(data);
            var parameters = JsonConvert.DeserializeObject<RSAParameters>(parameterString);

            rsa.ImportParameters(parameters);
        }
    }
}
