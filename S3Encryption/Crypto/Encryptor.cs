using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using System.IO;

namespace S3Encryption.Crypto
{
    public abstract partial class KMSCryptoTransform
    {
        public class Encryptor : KMSCryptoTransform
        {
            public Encryptor(IAmazonKeyManagementService client, string keyId)
                : base(client, keyId) { }

            public override byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                var request = new EncryptRequest
                {
                    KeyId = _keyId,
                    Plaintext = new MemoryStream(inputBuffer, inputOffset, inputCount)
                };

                var task = _client.EncryptAsync(request);

                task.Wait();
                return task.Result.CiphertextBlob.ToArray();
            }
        }

    }
}