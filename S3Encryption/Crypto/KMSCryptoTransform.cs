using Amazon.KeyManagementService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace S3Encryption.Crypto
{
    public abstract partial class KMSCryptoTransform : ICryptoTransform
    {
        protected IAmazonKeyManagementService _client;
        protected string _keyId;

        public KMSCryptoTransform(IAmazonKeyManagementService client)
        {
            this._client = client;
        }

        public KMSCryptoTransform(IAmazonKeyManagementService client, string keyId)
            : this(client)
        {
            this._keyId = keyId;
        }

        public bool CanReuseTransform
        {
            get { return true; }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return false; }
        }

        public int InputBlockSize
        {
            get { throw new NotImplementedException(); }
        }

        public int OutputBlockSize
        {
            get { throw new NotImplementedException(); }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            throw new NotImplementedException();
        }

        public abstract byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);

        public void Dispose()
        {

        }

    }
}