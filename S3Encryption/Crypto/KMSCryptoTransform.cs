using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace S3Encryption.Crypto
{
    public abstract class KMSCryptoTransform : ICryptoTransform
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

        public class Decryptor : KMSCryptoTransform
        {
            public Decryptor(IAmazonKeyManagementService client)
                : base(client) { }

            public override byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                //var response = Task.Factory.StartNew(() => _client.DecryptAsync(new DecryptRequest()
                //{
                //    CiphertextBlob = new MemoryStream(inputBuffer, inputOffset, inputCount)
                //}));


                var task = Task.Run<DecryptResponse>(() => {
                    return _client.DecryptAsync(new DecryptRequest()
                    {
                        CiphertextBlob = new MemoryStream(inputBuffer, inputOffset, inputCount)
                    });
                });


                task.Wait();
                return task.Result.Plaintext.ToArray();
            }
        }

        public class Encryptor : KMSCryptoTransform
        {
            public Encryptor(IAmazonKeyManagementService client, string keyId)
                : base(client, keyId) { }

            public override byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                var task = Task.Run<EncryptResponse>(() => {
                    return _client.EncryptAsync(new EncryptRequest()
                    {
                        KeyId = _keyId,
                        Plaintext = new MemoryStream(inputBuffer, inputOffset, inputCount)
                    });
                });


                task.Wait();
                return task.Result.CiphertextBlob.ToArray();
            }
        }

    }
}