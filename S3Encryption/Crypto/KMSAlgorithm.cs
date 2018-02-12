using Amazon.KeyManagementService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Crypto
{
    public class KMSAlgorithm : SymmetricAlgorithm
    {
        private IAmazonKeyManagementService _client;
        private string _keyId;

        public KMSAlgorithm(IAmazonKeyManagementService client)
        {
            this._client = client;
        }

        public KMSAlgorithm(IAmazonKeyManagementService client, string keyId)
            : this(client)
        {
            this._keyId = keyId;
        }

        public override ICryptoTransform CreateDecryptor()
        {
            return new KMSCryptoTransform.Decryptor(_client);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            throw new NotImplementedException();
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return new KMSCryptoTransform.Encryptor(_client, _keyId);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            throw new NotImplementedException();
        }

        public override void GenerateIV()
        {
            throw new NotImplementedException();
        }

        public override void GenerateKey()
        {
            throw new NotImplementedException();
        }
    }
}
