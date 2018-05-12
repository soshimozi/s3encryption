using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using System.IO;

namespace S3Encryption.Crypto
{
    public abstract partial class KMSCryptoTransform
    {
        public class Decryptor : KMSCryptoTransform
        {
            public Decryptor(IAmazonKeyManagementService client)
                : base(client) { }

            public override byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                var request = new DecryptRequest
                {
                    CiphertextBlob = new MemoryStream(inputBuffer, inputOffset, inputCount)
                };

                var task = _client.DecryptAsync(request);

                task.Wait();
                return task.Result.Plaintext.ToArray();
            }
        }

    }
}